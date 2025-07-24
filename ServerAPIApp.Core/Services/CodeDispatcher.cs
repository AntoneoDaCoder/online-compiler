using ServerAPIApp.Core.Abstractions;
using ServerAPIApp.Core.Repositories;
using Shared.DTOs;
using System.Threading.Channels;

namespace ServerAPIApp.Core.Services
{
    public class CodeDispatcher : ICodeDispatcher
    {
        private Channel<CodeRequestDto> _channel;
        private ProblemRepository _repository;

        private Dictionary<string, IKubernetesJobManager> _managers;
        private CancellationTokenSource? _cts;
        private Task? _consumeTask;
        private bool _isConsuming;
        private bool _disposed;

        public CodeDispatcher(IEnumerable<IKubernetesJobManager> managers, ProblemRepository repository)
        {
            _channel = Channel.CreateUnbounded<CodeRequestDto>
             (
                new UnboundedChannelOptions
                {
                    SingleReader = true,
                    SingleWriter = false,
                }
             );
            _managers = managers.ToDictionary(m => m.Language, StringComparer.OrdinalIgnoreCase);

            _repository = repository;
        }

        public async Task ScheduleForExecutionAsync(CodeRequestDto request, CancellationToken cancellationToken)
        {
            await _channel.Writer.WriteAsync(request, cancellationToken);
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
                    while (_channel.Reader.TryRead(out var request))
                    {
                        try
                        {
                            var problem = _repository.GetProblem(request.ProblemName);

                            var newSolution = new ProblemSolutionDto()
                            {
                                RequestId = request.RequestId,
                                Problem = problem,
                                Code = request.Code,
                                SentAt = request.RequestSentAt,
                                MaxAllowedTimeInMilliseconds = request.MaxAllowedTimeInMilliseconds,
                                CallbackUrl = request.CallbackUrl,
                                Language = request.Language,
                            };

                            if (!_managers.TryGetValue(request.Language, out var manager))
                                throw new NotSupportedException($"Language '{request.Language}' is not supported");

                            if (!await manager.ExecuteAsync(newSolution, cancellationToken))
                            {
                                await RescheduleExecution(request, cancellationToken);

                                Console.WriteLine("rescheduling execution?");
                            }
                        }
                        catch (Exception ex)
                        {
                            await RescheduleExecution(request, cancellationToken);

                            Console.WriteLine("rescheduling execution due to an exception? Exception: " + ex);
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

        private async Task RescheduleExecution(CodeRequestDto request, CancellationToken cancellationToken)
        {
            await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);
            await _channel.Writer.WriteAsync(request, cancellationToken);
        }
    }
}
