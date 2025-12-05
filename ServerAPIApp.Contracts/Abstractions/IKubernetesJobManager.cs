using Microsoft.Extensions.Hosting;
using Shared.DTOs;

namespace ServerAPIApp.Contracts.Abstractions
{
    public interface IKubernetesJobManager : IHostedService, IDisposable
    {
        string Language { get; }
        Task<bool> ExecuteAsync(ProblemSolutionDto request, CancellationToken cancellationToken);
        Task CompleteJobAsync(CodeResponseDto podResponse, CancellationToken cancellationToken);
    }
}
