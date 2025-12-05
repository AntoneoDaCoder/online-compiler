using Shared.DTOs;
using System.Diagnostics;

namespace Runners.Shared
{
    public interface IRunner : IDisposable
    {
        Task<CodeResponseDto> ExecuteCodeAsync(Guid requestId, DateTimeOffset requestDate, CancellationToken cancellationToken);
        Task<(bool Success, string CompilationErrors)> CompileCodeAsync(ProblemSolutionDto userSolution, CancellationToken cancellationToken);
    }
}
