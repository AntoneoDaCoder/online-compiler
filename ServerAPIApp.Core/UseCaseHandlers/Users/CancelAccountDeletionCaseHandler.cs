using Hangfire;
using MediatR;
using ServerAPIApp.Contracts.Abstractions;
using ServerAPIApp.Core.UseCases.Users;
using ServerAPIApp.Domain.Exceptions.ForbiddenExceptions;
using ServerAPIApp.Domain.Exceptions.NotFoundExceptions;

namespace ServerAPIApp.Core.UseCaseHandlers.Users
{
    public class CancelAccountDeletionCaseHandler : IRequestHandler<CancelAccountDeletionCase>
    {
        private readonly IUserRepository _repo;

        public CancelAccountDeletionCaseHandler(IUserRepository repo)
        {
            _repo = repo;
        }

        public async Task Handle(CancelAccountDeletionCase command, CancellationToken cancellationToken)
        {
            var parsedUserId = Guid.Parse(command.UserId);

            var entity = (await _repo.GetFilteredAsync(x => x.Id == parsedUserId, cancellationToken)).FirstOrDefault();

            if (entity == null)
                throw new ResourceNotFoundException("Account not found");

            if (entity.InitiatorId != command.SenderId)
                throw new ForbiddenException("You are not allowed to cancel account deletion that is not yours");

            if (entity.DeletionJobId is not null)
                BackgroundJob.Delete(entity.DeletionJobId);

            entity.DeletionJobId = null;
            entity.DeletionDeadline = null;
            entity.DeletionScheduledAt = null;
            entity.InitiatorId = null;
            entity.IsDeleted = false;

            await _repo.UpdateAsync(entity, cancellationToken);
        }
    }
}
