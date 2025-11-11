namespace ServerAPIApp.Domain.Entities
{
    public class ProblemVersionLanguagesEntity
    {
        public Guid VersionId { get; set; }
        public ProblemVersionEntity? Version { get; set; }
        public Guid LanguageId { get; set; }
        public LanguageEntity? Language { get; set; }
    }
}
