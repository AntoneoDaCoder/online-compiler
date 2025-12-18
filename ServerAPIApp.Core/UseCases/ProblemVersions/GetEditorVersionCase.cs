using MediatR;
using ServerAPIApp.Contracts.DTOs;

namespace ServerAPIApp.Core.UseCases.ProblemVersions
{
    public record GetEditorVersionCase(Guid VersionId) : IRequest<EditorProblemVersionDto?>;
}
