namespace ServerAPIApp.Domain.Entities
{
    public interface ISoftDeletable
    {
        bool IsDeleted { get; set; }
        DateTimeOffset? DeletionScheduledAt { get; set; }
        DateTimeOffset? DeletionDeadline { get; set; }
        Guid? InitiatorId { get; set; }
        UserEntity? Initiator { get; set; }
    }
}
