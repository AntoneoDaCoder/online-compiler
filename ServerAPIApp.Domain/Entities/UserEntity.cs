namespace ServerAPIApp.Domain.Entities
{
    public sealed class UserEntity : BaseEntity, ICreatable, IModifiable, ISoftDeletable
    {
        public string ExternalProviderId { get; set; }

        public DateTimeOffset? CreatedAt { get; set; }
        public Guid? CreatedBy { get; set; }
        public UserEntity? Creator { get; set; }

        public DateTimeOffset? ModifiedAt { get; set; }
        public Guid? ModifiedBy { get; set; }
        public UserEntity? Editor { get; set; }

        public string Name { get; set; }

        public bool IsDeleted { get; set; } = false;
        public DateTimeOffset? DeletionScheduledAt { get; set; }
        public DateTimeOffset? DeletionDeadline { get; set; }
        public Guid? InitiatorId { get; set; }
        public UserEntity? Initiator { get; set; }
    }
}
