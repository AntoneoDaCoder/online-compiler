using Shared.DTOs;
using Shared.Enums;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Runners.Shared
{
    public class RunnerBase : IDisposable
    {
        const string _apiCallbackUrl = "http://api-server.default.svc.cluster.local:8080/api/jobs/complete";

        private static JsonSerializerOptions _options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        private HttpListener _listener = new HttpListener();
        private HttpClient _client = new HttpClient();
        private IRunner _runner;
        private bool _isDisposed;

        public RunnerBase(IRunner runner)
        {
            _runner = runner;
            _listener.Prefixes.Add("http://*:5000/run/");
            _listener.Start();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public async Task ListenAsync(CancellationToken cancellationToken = default)
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

                            _ = Task.Run(async () =>
                            {
                                try
                                {
                                    await ExecuteUserCodeAsync(codeRequest, cancellationToken);
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"[Runner] Failed to execute code: {ex}");
                                }
                            }, cancellationToken);

                            Console.WriteLine($"[Runner] Scheduled request [Id:{codeRequest.RequestId}]");
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

        private async Task NotifyJobManagerAsync(CodeResponseDto response, string callbackUrl, Guid requestId, CancellationToken cancellationToken = default)
        {
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

        private async Task ExecuteUserCodeAsync(ProblemSolutionDto request, CancellationToken cancellationToken = default)
        {
            Console.WriteLine("[Runner] Compiling code");

            var compilationResult = await _runner.CompileCodeAsync(request, cancellationToken);

            if (!compilationResult.Success)
            {
                var result = new CodeResponseDto()
                {
                    RequestId = request.RequestId,
                    Status = RequestStatus.Failed,
                    VersionId = request.VersionId,
                    UserId = request.UserId,
                    UserSolution = request.UserSolution,
                    Language = request.LanguageCode,
                    Result = new ExecutionResultDto()
                    {
                        Status = ExecutionStatus.CompileError,
                        ExitCode = 1,
                        ConsoleOutput = compilationResult.CompilationErrors,
                        PassedTests = 0,
                        TotalTests = compilationResult.TotalTests,
                        RequestSentAt = request.SentAt,
                        ResponseSentAt = DateTimeOffset.UtcNow,
                    }
                };

                await NotifyJobManagerAsync(result, _apiCallbackUrl, request.RequestId, cancellationToken);

                return;
            }

            Console.WriteLine("[Runner] Code has been compiled, ready to execute it");

            var executionData = new ExecutionData()
            {
                RequestId = request.RequestId,
                RequestDate = request.SentAt,
                Language = request.LanguageCode,
                UserId = request.UserId,
                VersionId = request.VersionId,
                UserSolution = request.UserSolution,
                TotalTests = compilationResult.TotalTests,
                ExecutablePath = compilationResult.ExecutablePath
            };

            var executionResult = await _runner.ExecuteCodeAsync(executionData, cancellationToken);

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
                _runner.Dispose();

                _client.Dispose();
                _listener.Close();
            }
            _isDisposed = true;
        }
    }
}
