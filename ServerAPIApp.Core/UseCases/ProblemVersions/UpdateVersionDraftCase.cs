using MediatR;
using ServerAPIApp.Contracts.DTOs;

namespace ServerAPIApp.Core.UseCases.ProblemVersions
{
    public record UpdateVersionDraftCase(Guid DraftId, Guid CreatedBy, string Statement, int TotalTests, ManifestDto TestManifest) : IRequest;
}
