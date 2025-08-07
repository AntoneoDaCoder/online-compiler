using Shared.DTOs;
using System.Diagnostics;

namespace Runners.Shared
{
    public interface IRunner : IDisposable
    {
        Task<CodeResponseDto> ExecuteCodeAsync(Guid requestId, DateTime requestDate, CancellationToken cancellationToken);
        Task<(bool Success, string CompilationErrors)> CompileCodeAsync(string fullCode, CancellationToken cancellationToken);
        string WrapCode(ProblemSolutionDto problemSolutionDto);
    }
}
