using k8s;
using Microsoft.Extensions.Options;
using ServerAPIApp.Contracts.Abstractions;
using ServerAPIApp.Configs;
using Shared.DTOs;

namespace ServerAPIApp.Dispatchers
{
    public class CompositeKubernetesJobManager : IKubernetesJobManager, IHostedService, IDisposable
    {
        public string Language => "composite";

        private readonly KubernetesJobManager _inner;
        private readonly HashSet<string> _supportedLanguages = new(StringComparer.OrdinalIgnoreCase);

        public CompositeKubernetesJobManager(
            IKubernetes client,
            IOptionsMonitor<LanguageConfig> config)
        {
            _inner = new KubernetesJobManager("composite", client, config);
        }

        public void RegisterLanguage(string language)
        {
            _supportedLanguages.Add(language);
        }

        public bool CanHandle(string language) => _supportedLanguages.Contains(language);

        public Task StartAsync(CancellationToken cancellationToken) =>
            _inner.StartAsync(cancellationToken);

        public Task StopAsync(CancellationToken cancellationToken) =>
            _inner.StopAsync(cancellationToken);

        public Task<bool> ExecuteAsync(ProblemSolutionDto request, CancellationToken token)
        {
            if (!CanHandle(request.Language))
                throw new NotSupportedException($"Language '{request.Language}' is not supported by composite manager");

            return _inner.ExecuteAsync(request, token);
        }

        public Task CompleteJobAsync(CodeResponseDto response, CancellationToken cancellationToken) =>
            _inner.CompleteJobAsync(response, cancellationToken);

        public void Dispose() => _inner.Dispose();
    }
}
