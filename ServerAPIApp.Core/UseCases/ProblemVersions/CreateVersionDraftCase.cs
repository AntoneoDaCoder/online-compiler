using MediatR;
using Shared.DTOs;
using ServerAPIApp.Domain.Entities;

namespace ServerAPIApp.Core.UseCases.ProblemVersions
{
    public record CreateVersionDraftCase(Guid ProblemId, Guid CreatedBy, string Statement, int TotalTests, string? TestManifestJson) : IRequest<ProblemVersionEntity>;
}
