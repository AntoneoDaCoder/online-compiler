using MediatR;
using ServerAPIApp.Contracts.DTOs;

namespace ServerAPIApp.Core.UseCases.ProblemVersions
{
    public record GetVersionByIdForExecutionCase(Guid VersionId, string LanguageCode) : IRequest<ExecutionDto>;
}
