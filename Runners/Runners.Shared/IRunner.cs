using Shared.DTOs;
using System.Diagnostics;

namespace Runners.Shared
{
    public interface IRunner : IDisposable
    {
        Task<CodeResponseDto> ExecuteCodeAsync(Guid requestId, DateTime requestDate, ProcessStartInfo pInfo, CancellationToken cancellationToken);
        (bool Success, string CompilationErrors) CompileCode(string fullCode, out ProcessStartInfo? pInfo, CancellationToken cancellationToken);
        string WrapCode(ProblemSolutionDto problemSolutionDto);
    }
}
