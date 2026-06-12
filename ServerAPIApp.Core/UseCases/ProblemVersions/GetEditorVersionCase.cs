using MediatR;
using ServerAPIApp.Contracts.DTOs.Problems;

namespace ServerAPIApp.Core.UseCases.ProblemVersions
{
    public record GetEditorVersionCase(Guid VersionId) : IRequest<EditorProblemVersionDto?>;
}
