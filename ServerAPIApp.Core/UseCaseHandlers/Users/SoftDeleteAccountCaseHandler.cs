using Hangfire;
using MediatR;
using ServerAPIApp.Contracts.Abstractions;
using ServerAPIApp.Contracts.DTOs;
using ServerAPIApp.Core.Abstractions;
using ServerAPIApp.Core.UseCases.Users;
using ServerAPIApp.Domain.Constants;
using ServerAPIApp.Domain.Exceptions.ForbiddenExceptions;

namespace ServerAPIApp.Core.UseCaseHandlers.Users
{
    public class SoftDeleteAccountCaseHandler : IRequestHandler<SoftDeleteAccountCase, UserMetadataDto?>
    {
        private readonly IUserRepository _repo;

        public SoftDeleteAccountCaseHandler(IUserRepository repo)
        {
            _repo = repo;
        }

        public async Task<UserMetadataDto?> Handle(SoftDeleteAccountCase command, CancellationToken cancellationToken)
        {
            var userId = Guid.Parse(command.UserId);
            if (userId != command.SenderId)
                throw new ForbiddenException("You're not allowed to delete someone else's account");

            var entity = (await _repo.GetFilteredAsync(x => x.Id == userId, cancellationToken)).FirstOrDefault();

            if (entity is null)
                return null;

            var deletionInitiationDate = DateTimeOffset.UtcNow;
            var deletionDate = deletionInitiationDate.Add(ApplicationConstants.GracePeriod);

            entity.DeletionScheduledAt = deletionInitiationDate;
            entity.DeletionDeadline = deletionDate;
            entity.InitiatorId = userId;
            entity.IsDeleted = true;

            string newJobId = BackgroundJob.Schedule<IUserRepository>
                (svc => svc.DeleteAsync(entity),
                 ApplicationConstants.GracePeriod);

            BackgroundJob.ContinueJobWith<ICleanupService>(newJobId,
                    svc => svc.DeleteUserRelatedMetadataAsync(command.UserId, newJobId),
                    JobContinuationOptions.OnlyOnSucceededState);

            entity.DeletionJobId = newJobId;

            await _repo.UpdateAsync(entity, cancellationToken);

            return UserMetadataDto.From(entity);
        }
    }
}
