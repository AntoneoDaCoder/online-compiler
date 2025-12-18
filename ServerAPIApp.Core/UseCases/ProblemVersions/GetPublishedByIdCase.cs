using MediatR;
using ServerAPIApp.Contracts.DTOs;

namespace ServerAPIApp.Core.UseCases.ProblemVersions
{
    public record GetPublishedByIdCase(Guid VersionId) : IRequest<UserProblemVersionDto?>;
}
