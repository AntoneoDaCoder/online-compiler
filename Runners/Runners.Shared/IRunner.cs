using Shared.DTOs;

namespace Runners.Shared
{
    public interface IRunner : IDisposable
    {
        Task<CodeResponseDto> ExecuteCodeAsync(ExecutionData data, CancellationToken cancellationToken = default);
        Task<CompilationResult> CompileCodeAsync(ProblemSolutionDto userSolution, CancellationToken cancellationToken = default);
    }
}
