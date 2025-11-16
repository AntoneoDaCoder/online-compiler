namespace ServerAPIApp.Domain.Entities
{
    public class ProblemVersionEntity : BaseEntity, ICreatable, IPublishable
    {
        public Guid ProblemId { get; set; }
        public ProblemEntity? Problem { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
        public Guid CreatedBy { get; set; }
        public UserEntity? Creator { get; set; }

        public bool IsDraft { get; set; } = true;

        public bool IsPublished { get; set; } = false;
        public Guid? PublishedBy { get; set; }
        public UserEntity? Publisher { get; set; }

        public int Version { get; set; }
        public string Statement { get; set; } = "Sample problem statement";
        public int TotalTests { get; set; }
        public string TestTemplateKey { get; set; } = default!;
        public int NumSubmissions { get; set; }

        public ICollection<ProblemVersionLanguage> SupportedLanguages { get; set; } = new List<ProblemVersionLanguage>();
    }
}
