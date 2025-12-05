using ServerAPIApp.Contracts.Abstractions;
using Shared.DTOs;

namespace ServerAPIApp.Dispatchers
{
    public class CompositeJobManagerProxy : IKubernetesJobManager
    {
        private readonly CompositeKubernetesJobManager _inner;

        public string Language { get; }

        public CompositeJobManagerProxy(string language, CompositeKubernetesJobManager inner)
        {
            Language = language;
            _inner = inner;
        }

        public Task<bool> ExecuteAsync(ProblemSolutionDto request, CancellationToken token) =>
            _inner.ExecuteAsync(request, token);

        public Task CompleteJobAsync(CodeResponseDto response, CancellationToken cancellationToken) =>
            _inner.CompleteJobAsync(response, cancellationToken);

        public Task StartAsync(CancellationToken cancellationToken) => _inner.StartAsync(cancellationToken);

        public Task StopAsync(CancellationToken cancellationToken) => _inner.StopAsync(cancellationToken);

        public void Dispose() => _inner.Dispose();
    }
}
