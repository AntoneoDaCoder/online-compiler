using MediatR;
using ServerAPIApp.Contracts.Abstractions;
using ServerAPIApp.Contracts.DTOs.Problems;
using ServerAPIApp.Core.Helpers;
using ServerAPIApp.Core.UseCases.Problems;
using ServerAPIApp.Domain.Entities;
using System.Linq.Expressions;
using ServerAPIApp.Domain.Exceptions.BadRequestExceptions;

namespace ServerAPIApp.Core.UseCaseHandlers.Problems
{
    public class GetFilteredProblemDeletionRequestsCaseHandler : IRequestHandler<GetFilteredProblemDeletionRequestsCase, IEnumerable<ProblemDeletionRequestDto>>
    {
        private readonly IProblemDeletionRequestRepository _repo;

        public GetFilteredProblemDeletionRequestsCaseHandler(IProblemDeletionRequestRepository repo)
        {
            _repo = repo;
        }

        public async Task<IEnumerable<ProblemDeletionRequestDto>> Handle(GetFilteredProblemDeletionRequestsCase request, CancellationToken cancellationToken)
        {
            var filter = BuildFilter(request);

            var entities = await _repo.GetFilteredAsync(filter, cancellationToken, includes: x => x.Problem);

            if (entities is null)
                entities = [];

            return entities.ToDto();
        }

        private static Expression<Func<ProblemDeletionRequestEntity, bool>> BuildFilter(GetFilteredProblemDeletionRequestsCase request)
        {
            Expression<Func<ProblemDeletionRequestEntity, bool>> filter = x => true;

            if (request.OnlyNotApproved.HasValue && request.OnlyNotApproved.Value)
            {
                filter = filter.And(x => !x.IsApproved);
            }

            if (request.ExcludeUser.HasValue && request.ExcludeUser.Value)
            {
                if (!request.UserId.HasValue)
                    throw new IncorrectFilterException("To exclude user, UserId has to be provided");
                else
                {
                    filter = filter.And(x => x.InitiatorId != request.UserId.Value);
                }
            }

            if (request.OnlyUser.HasValue && request.OnlyUser.Value)
            {
                if (!request.UserId.HasValue)
                    throw new IncorrectFilterException("To include user, UserId has to be provided");
                else
                {
                    filter = filter.And(x => x.InitiatorId == request.UserId.Value);
                }
            }

            if (request.ExactMatch.HasValue && request.ExactMatch.Value)
            {
                if (!request.ProblemId.HasValue)
                    throw new IncorrectFilterException("To find the exact match by a linked problem, ProblemId has to be provided");
                else
                {
                    filter = filter.And(x => x.ProblemId == request.ProblemId.Value);
                }

            }

            return filter;
        }
    }
}
