namespace ServerAPIApp.Domain.Entities
{
    public class ProblemVersionEntity : BaseEntity, ICreatable, IPublishable
    {
        public Guid ProblemId { get; set; } //send but don't render
        public ProblemEntity? Problem { get; set; } //don't send to ui

        public DateTimeOffset CreatedAt { get; set; }
        public Guid CreatedBy { get; set; }
        public UserEntity? Creator { get; set; } //don't send to ui

        public bool IsDraft { get; set; } = true;

        public bool IsPublished { get; set; } = false;
        public Guid? PublishedBy { get; set; }
        public UserEntity? Publisher { get; set; } //don't send to ui

        public int Version { get; set; } //don't send to ui
        public string Statement { get; set; } = "Sample problem statement";
        public int TotalTests { get; set; }
        public string TestTemplateKey { get; set; } = default!; //don't send to ui
        public int NumSubmissions { get; set; } //don't send to ui

        public ICollection<ProblemVersionLanguage> SupportedLanguages { get; set; } = new List<ProblemVersionLanguage>();
    }
}
