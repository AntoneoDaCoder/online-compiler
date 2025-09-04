using Shared.DTOs;
using Shared.Enums;
using Shared.Models;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Channels;

namespace Runners.Shared
{
    public class CompositeRunner : IDisposable
    {
        const string _apiCallbackUrl = "http://api-server.default.svc.cluster.local:8080/api/jobs/complete";

        private static JsonSerializerOptions _options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };


        private HttpListener _listener = new HttpListener();
        private HttpClient _client = new HttpClient();
        private FrozenDictionary<string, IRunner> _runners;
        private Channel<ProblemSolutionDto> _pendingRequests;
        private CancellationTokenSource? _cts;
        private Task? _consumeTask;
        private bool _isConsuming;
        private bool _isDisposed;

        public CompositeRunner(Dictionary<string, IRunner> runners)
        {
            _listener.Prefixes.Add("http://*:5000/run/");
            _listener.Start();
            _runners = runners.ToFrozenDictionary();

            _pendingRequests = Channel.CreateBounded<ProblemSolutionDto>(
                    new BoundedChannelOptions(100) 
                    {
                        SingleReader = true,
                        SingleWriter = false,
                        FullMode = BoundedChannelFullMode.Wait
                    });
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            if (_isConsuming)
            {
                return Task.CompletedTask;
            }

            _isConsuming = true;

            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _consumeTask = ConsumeAsync(_cts.Token);

            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (!_isConsuming)
            {
                return;
            }

            _isConsuming = false;

            _cts?.Cancel();

            if (_consumeTask is not null)
                await _consumeTask;

            _cts?.Dispose();
            _consumeTask = null;
            _cts = null;
            _pendingRequests.Writer.Complete();
        }


        public async Task ListenAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var context = await _listener.GetContextAsync();

                    using (var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding))
                    {
                        try
                        {
                            var requestString = await reader.ReadToEndAsync(cancellationToken);

                            var codeRequest = JsonSerializer.Deserialize<ProblemSolutionDto>(requestString, _options);

                            Console.WriteLine($"[Runner] Received request [Id:{codeRequest.RequestId}]");

                            await ScheduleForExecutionAsync(codeRequest, cancellationToken);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error processing callback: {ex}");
                        }
                        finally
                        {
                            context.Response.Close();
                        }
                    }
                }
            }
            catch (HttpListenerException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Runner] Error in listening loop: {ex}");
            }
        }

        private async Task ScheduleForExecutionAsync(ProblemSolutionDto request, CancellationToken cancellationToken)
        {
            await _pendingRequests.Writer.WriteAsync(request, cancellationToken);
        }

        private async Task NotifyJobManagerAsync(CodeResponseDto response, string callbackUrl, Guid requestId, CancellationToken cancellationToken)
        {
            response.Result.ResponseSentAt = DateTime.UtcNow;

            try
            {
                await _client.PostAsJsonAsync(callbackUrl, response, cancellationToken);

                Console.WriteLine($"[Runner] Sent response [Id:{response.RequestId}] to request [Id:{requestId}]");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Runner] Failed to send response [Id:{requestId}]: {ex}");
            }
        }

        private async Task ConsumeAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (await _pendingRequests.Reader.WaitToReadAsync(cancellationToken))
                {
                    while (_pendingRequests.Reader.TryRead(out var request))
                    {
                        try
                        {
                            await ExecuteUserCodeAsync(request, cancellationToken);

                            Console.WriteLine($"[Runner] Scheduled request [Id:{request.RequestId}]");
                        }
                        catch (Exception ex)
                        {
                            await RescheduleExecution(request, cancellationToken);

                            Console.WriteLine("[CompositeRunner] Rescheduling execution due to an exception: " + ex);
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // expected shut down
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CodeRunner] Unexpected error: {ex}");
            }
        }

        private async Task RescheduleExecution(ProblemSolutionDto request, CancellationToken cancellationToken)
        {
            await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);
            await _pendingRequests.Writer.WriteAsync(request, cancellationToken);
        }

        private async Task ExecuteUserCodeAsync(ProblemSolutionDto request, CancellationToken cancellationToken)
        {
            if (!_runners.TryGetValue(request.Language, out var runner))
            {

                var result = new CodeResponseDto()
                {
                    RequestId = request.RequestId,
                    Language = request.Language,
                    Status = RequestStatus.Failed,
                    Result = new ExecutionResultDto()
                    {
                        RequestSentAt = request.SentAt,
                        Status = ExecutionStatus.FailedToExecute,
                        ExitCode = 1,
                        ConsoleOutput = $"Unsupported language: {request.Language}"
                    }
                };


                await NotifyJobManagerAsync(result, _apiCallbackUrl, request.RequestId, cancellationToken);

                return;
            }


            var wrappedCode = runner.WrapCode(request);

            Console.WriteLine("[Runner] Compiling code");

            var compilationResult = await runner.CompileCodeAsync(wrappedCode, cancellationToken);

            if (!compilationResult.Success)
            {
                var result = new CodeResponseDto()
                {
                    RequestId = request.RequestId,
                    Language = request.Language,
                    Status = RequestStatus.Failed,
                    Result = new ExecutionResultDto()
                    {
                        RequestSentAt = request.SentAt,
                        Status = ExecutionStatus.CompileError,
                        ExitCode = 1,
                        ConsoleOutput = compilationResult.CompilationErrors
                    }
                };

                await NotifyJobManagerAsync(result, _apiCallbackUrl, request.RequestId, cancellationToken);

                return;
            }

            Console.WriteLine("[Runner] Code has been compiled, ready to execute it");

            var executionResult = await runner.ExecuteCodeAsync(request.RequestId, request.SentAt, cancellationToken);

            Console.WriteLine("[Runner] Got the execution results, sending them back to the client");

            await NotifyJobManagerAsync(executionResult, _apiCallbackUrl, request.RequestId, cancellationToken);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed)
            {
                return;
            }
            if (disposing)
            {
                _cts?.Cancel();

                if (_consumeTask is not null)
                {
                    try
                    {
                        _consumeTask.GetAwaiter().GetResult();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[CompositeRunner] Error while disposing consume task: {ex}");
                    }
                }

                _cts?.Dispose();
                _consumeTask = null;
                _cts = null;
                _isConsuming = false;

                foreach (var (_, runner) in _runners)
                    runner.Dispose();

                _client.Dispose();
                _listener.Close();
            }
            _isDisposed = true;
        }
    }
}
