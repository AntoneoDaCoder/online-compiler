namespace ServerAPIApp.Domain.Entities
{
    public class ProblemEntity : BaseEntity, ISoftDeletable, ICreatable, IModifiable
    {
        public string Slug { get; set; } = "SAMPLE-1234";
        public string Title { get; set; } = "Sample Title";

        public DateTimeOffset? CreatedAt { get; set; } = DateTimeOffset.MinValue;
        public Guid? CreatedBy { get; set; } = Guid.Empty;
        public UserEntity? Creator { get; set; }


        public bool IsDeleted { get; set; } = false;
        public DateTimeOffset? DeletionScheduledAt { get; set; }
        public DateTimeOffset? DeletionDeadline { get; set; }
        public string? DeletionJobId { get; set; }

        public DateTimeOffset? ModifiedAt { get; set; }
        public Guid? ModifiedBy { get; set; }
        public UserEntity? Editor { get; set; }

        public Guid? LastPublishedVersionId { get; set; }
        public ProblemVersionEntity? LastPublishedVersion { get; set; }

        public ICollection<ProblemVersionEntity> Versions { get; set; } = new List<ProblemVersionEntity>();
    }
}
