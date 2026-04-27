namespace ServerAPIApp.Domain.Entities
{
    public sealed class UserEntity : BaseEntity, IModifiable, ISoftDeletable
    {
        public string ExternalProviderId { get; set; }

        public DateTimeOffset? ModifiedAt { get; set; }
        public Guid? ModifiedBy { get; set; }
        public UserEntity? Editor { get; set; }

        public bool IsDeleted { get; set; } = false;
        public string? DeletionJobId { get; set; }

        public DateTimeOffset? DeletionScheduledAt { get; set; }
        public DateTimeOffset? DeletionDeadline { get; set; }
        public Guid? InitiatorId { get; set; }
        public UserEntity? Initiator { get; set; }
    }
}
