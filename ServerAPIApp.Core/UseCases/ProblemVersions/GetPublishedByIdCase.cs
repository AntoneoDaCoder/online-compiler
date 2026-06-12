using MediatR;
using ServerAPIApp.Contracts.DTOs.Problems;

namespace ServerAPIApp.Core.UseCases.ProblemVersions
{
    public record GetPublishedByIdCase(Guid VersionId) : IRequest<UserProblemVersionDto?>;
}
