using Shared.DTOs;

namespace Runners.Shared.Runners
{
    public class TypeScriptRunner : IRunner
    {
        public Task<(bool Success, string CompilationErrors)> CompileCodeAsync(string fullCode, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public Task<CodeResponseDto> ExecuteCodeAsync(Guid requestId, DateTime requestDate, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public string WrapCode(ProblemSolutionDto problemSolutionDto)
        {
            throw new NotImplementedException();
        }
    }
}
