using MediatR;
using ServerAPIApp.Contracts.DTOs;

namespace ServerAPIApp.Core.UseCases.ProblemVersions
{
    public record GetVersionDraftsByIdCase(Guid ProblemId) : IRequest<DraftListDto>;
}
