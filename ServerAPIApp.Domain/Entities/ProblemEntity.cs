namespace ServerAPIApp.Domain.Entities
{
    public class ProblemEntity : BaseEntity, ISoftDeletable, ICreatable, IModifiable
    {
        public string Slug { get; set; } = "SAMPLE-1234";
        public string Title { get; set; } = "Sample Title";
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset ModifiedAt { get; set; }
        public bool IsDeleted { get; set; }
        public ICollection<ProblemVersionEntity> Versions { get; set; } = new List<ProblemVersionEntity>();
    }
}
