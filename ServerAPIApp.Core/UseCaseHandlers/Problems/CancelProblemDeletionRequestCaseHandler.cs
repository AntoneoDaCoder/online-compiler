using MediatR;
using ServerAPIApp.Contracts.Abstractions;
using ServerAPIApp.Core.UseCases.Problems;
using ServerAPIApp.Domain.Exceptions.ForbiddenExceptions;
using ServerAPIApp.Domain.Constants;

namespace ServerAPIApp.Core.UseCaseHandlers.Problems
{
    public class CancelProblemDeletionRequestCaseHandler : IRequestHandler<CancelProblemDeletionRequestCase, Guid?>
    {
        private readonly IProblemDeletionRequestRepository _repo;

        public CancelProblemDeletionRequestCaseHandler(IProblemDeletionRequestRepository repo)
        {
            _repo = repo;
        }

        public async Task<Guid?> Handle(CancelProblemDeletionRequestCase request, CancellationToken cancellationToken)
        {
            var entity = (await _repo.GetFilteredAsync(x => x.Id == request.RequestId, cancellationToken)).FirstOrDefault();

            if (entity == null)
                return null;

            if (entity.InitiatorId != request.SenderId && !request.SenderRoles.Any(x => x.Contains(UserRelatedConstants.AdminRoleName)))
                throw new ForbiddenException("You are not allowed to cancel a request that is not yours");

            await _repo.DeleteAsync(entity, cancellationToken);

            return entity.InitiatorId;
        }
    }
}
