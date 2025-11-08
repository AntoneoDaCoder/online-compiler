namespace ServerAPIApp.Domain.Entities
{
    public interface ISoftDeletable
    {
        bool IsDeleted { get; set; }
        DateTimeOffset? DeletionScheduledAt { get; set; }
        DateTimeOffset? DeletionDeadline { get; set; }
        UserEntity? Initiator { get; set; }
    }
}
