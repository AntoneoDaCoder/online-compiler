using MediatR;
using ServerAPIApp.Contracts.DTOs.Problems;

namespace ServerAPIApp.Core.UseCases.Problems
{
    public record GetFilteredProblemDeletionRequestsCase(bool? ExcludeUser = null, bool? OnlyUser = null, Guid? UserId = null, bool? ExactMatch = null, Guid? ProblemId = null) :
        IRequest<IEnumerable<ProblemDeletionRequestDto>>;
}
