using ServerAPIApp.Domain.Entities;

namespace ServerAPIApp.Contracts.DTOs
{
    public record UserMetadataDto(Guid UserId, bool IsDeleted, DateTimeOffset? DeletionScheduledAt, DateTimeOffset? DeletionDeadline)
    {
        public static UserMetadataDto From(UserEntity entity)
        {
            return new UserMetadataDto(entity.Id, entity.IsDeleted, entity.DeletionScheduledAt, entity.DeletionDeadline);
        }
    }
}
