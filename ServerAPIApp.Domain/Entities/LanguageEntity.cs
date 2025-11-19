namespace ServerAPIApp.Domain.Entities
{
    public class LanguageEntity : BaseEntity
    {
        public string DisplayName { get; set; } = default!;
        public string Code { get; set; } = default!;
        public ICollection<ProblemVersionLanguage>? ProblemVersionLinks { get; set; } = new List<ProblemVersionLanguage>();
    }
}
