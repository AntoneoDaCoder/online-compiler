using MediatR;
using ServerAPIApp.Contracts.DTOs;

namespace ServerAPIApp.Core.UseCases.ProblemVersions
{
    public record GetVersionDraftByIdCase(Guid VersionId) : IRequest<EditorProblemVersionDto>;
}
