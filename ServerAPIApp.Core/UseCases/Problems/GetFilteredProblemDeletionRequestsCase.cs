using MediatR;
using ServerAPIApp.Contracts.DTOs.Problems;
using ServerAPIApp.Domain.Entities;
using System.Linq.Expressions;

namespace ServerAPIApp.Core.UseCases.Problems
{
    public record GetFilteredProblemDeletionRequestsCase(Expression<Func<ProblemDeletionRequestEntity, bool>> Filter/*, int Page, int PageSize*/) : IRequest<IEnumerable<ProblemDeletionRequestDto>>;
}
