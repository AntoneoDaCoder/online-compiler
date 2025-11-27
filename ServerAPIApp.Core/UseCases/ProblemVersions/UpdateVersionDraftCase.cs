using MediatR;
using ServerAPIApp.Contracts.DTOs;

namespace ServerAPIApp.Core.UseCases.ProblemVersions
{
    public record UpdateVersionDraftCase(Guid VersionId, Guid ProblemId, string Statement, int TotalTests, ManifestDto? TestManifest) : IRequest;
}
