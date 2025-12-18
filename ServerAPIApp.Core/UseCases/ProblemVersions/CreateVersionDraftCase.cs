using MediatR;
using Shared.DTOs;
using ServerAPIApp.Domain.Entities;
using ServerAPIApp.Contracts.DTOs;

namespace ServerAPIApp.Core.UseCases.ProblemVersions
{
    public record CreateVersionDraftCase(Guid ProblemId, Guid CreatedBy, string Statement, int TotalTests, ManifestDto? TestManifest) : IRequest<EditorProblemVersionDto>;
}
