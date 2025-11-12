namespace ServerAPIApp.Domain.Entities
{
    public class ProblemVersionLanguage
    {
        public Guid VersionId { get; set; }
        public ProblemVersionEntity? Version { get; set; }
        public Guid LanguageId { get; set; }
        public LanguageEntity? Language { get; set; }
        public string? ArtifactsKey { get; set; }
        public string EntryPoint { get; set; } = default!;
    }
}
