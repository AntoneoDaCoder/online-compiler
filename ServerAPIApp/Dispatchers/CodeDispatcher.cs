using MediatR;
using ServerAPIApp.Contracts.DTOs;
using ServerAPIApp.Contracts.Abstractions;
using ServerAPIApp.Core.UseCases.ProblemVersions;
using Shared.DTOs;
using System.Threading.Channels;

namespace ServerAPIApp.Dispatchers
{
    public class CodeDispatcher : ICodeDispatcher
    {
        private const int MaxRetries = 10;

        private Channel<RequestData> _channel;

        private Dictionary<string, IKubernetesJobManager> _managers;
        private IServiceScopeFactory _scopeFactory;
        private CancellationTokenSource? _cts;
        private Task? _consumeTask;
        private bool _isConsuming;
        private bool _disposed;

        public CodeDispatcher(IEnumerable<IKubernetesJobManager> managers, IServiceScopeFactory scopeFactory)
        {
            _channel = Channel.CreateUnbounded<RequestData>
             (
                new UnboundedChannelOptions
                {
                    SingleReader = true,
                    SingleWriter = false,
                }
             );
            _managers = managers.ToDictionary(m => m.Language, StringComparer.OrdinalIgnoreCase);
            _scopeFactory = scopeFactory;
        }

        public async Task ScheduleForExecutionAsync(Guid userId, CodeRequestDto request, CancellationToken cancellationToken)
        {
            await _channel.Writer.WriteAsync(new RequestData(userId, request), cancellationToken);
        }

        public async Task CompleteExecutionAsync(CodeResponseDto response, CancellationToken cancellationToken)
        {
            if (!_managers.TryGetValue(response.Language, out var manager))
                throw new NotSupportedException($"Language '{response.Language}' is not supported");

            await manager.CompleteJobAsync(response, cancellationToken);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            if (_isConsuming)
            {
                return Task.CompletedTask;
            }

            _isConsuming = true;

            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _consumeTask = Task.Run(() => ConsumeAsync(_cts.Token), _cts.Token);

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
            _channel.Writer.Complete();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
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
                        Console.WriteLine($"[CodeDispatcher] Error while disposing consume task: {ex}");
                    }
                }

                _cts?.Dispose();
                _consumeTask = null;
                _cts = null;
                _isConsuming = false;

                // _channel.Writer.Complete();
            }

            _disposed = true;
        }

        private async Task ConsumeAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (await _channel.Reader.WaitToReadAsync(cancellationToken))
                {
                    while (_channel.Reader.TryRead(out var requestData))
                    {
                        try
                        {
                            await using var scope = _scopeFactory.CreateAsyncScope();

                            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                            var notifier = scope.ServiceProvider.GetRequiredService<ISubmissionNotifier>();

                            if (requestData.NumRetries > MaxRetries)
                            {
                                await notifier.NotifyFailedAsync(requestData.Body.RequestId, "Exceeded maximum number of retries", cancellationToken);

                                continue;
                            }

                            try
                            {
                                var request = requestData.Body;
                                var userId = requestData.UserId;

                                var command = new GetValidatedVersionManifestByIdCase(request.ProblemVersionId, request.LanguageCode);

                                var manifestString = await mediator.Send(command, cancellationToken);

                                var newSolution = new ProblemSolutionDto()
                                {
                                    RequestId = request.RequestId,
                                    UserSolution = request.Code,
                                    SentAt = request.RequestSentAt,
                                    LanguageCode = request.LanguageCode,
                                    TestManifestJson = manifestString,
                                };

                                if (!_managers.TryGetValue(request.LanguageCode, out var manager))
                                    throw new NotSupportedException($"Language '{request.LanguageCode}' is not supported");

                                if (!await manager.ExecuteAsync(newSolution, cancellationToken))
                                {
                                    requestData.NumRetries++;

                                    await RescheduleExecution(requestData, cancellationToken);

                                    Console.WriteLine("Rescheduling execution for request {0}, manager failed to send the request", request.RequestId);
                                }
                            }
                            catch (ApplicationException ex)
                            {
                                requestData.NumRetries++;

                                await RescheduleExecution(requestData, cancellationToken);

                                Console.WriteLine("Rescheduling execution for request {0}, application exception occured: {1}", requestData.Body.RequestId, ex);
                            }
                        }
                        catch (Exception ex)
                        {
                            requestData.NumRetries++;

                            await RescheduleExecution(requestData, cancellationToken);

                            Console.WriteLine("Rescheduling execution for request {0}, reason: {1} ", requestData.Body.RequestId, ex);
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

        private async Task RescheduleExecution(RequestData data, CancellationToken cancellationToken)
        {
            await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);

            await _channel.Writer.WriteAsync(data, cancellationToken);
        }

        private class RequestData
        {
            public Guid UserId { get; set; }
            public CodeRequestDto Body { get; set; }
            public int NumRetries { get; set; } = 0;

            public RequestData(Guid userId, CodeRequestDto body)
            {
                UserId = userId;
                Body = body;
            }
        }
    }
}
