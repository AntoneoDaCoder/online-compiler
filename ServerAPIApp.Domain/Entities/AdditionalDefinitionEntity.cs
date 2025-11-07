namespace ServerAPIApp.Domain.Entities
{
    public class AdditionalDefinitionEntity : BaseEntity
    {
        public Guid ProblemVersionId { get; set; }
        public ProblemVersionEntity? ProblemVersion { get; set; }
        public string Language { get; set; } = string.Empty; //should be indexed
        public string Value { get; set; } = string.Empty;
    }
}
