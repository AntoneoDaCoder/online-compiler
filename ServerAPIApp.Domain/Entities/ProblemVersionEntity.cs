namespace ServerAPIApp.Domain.Entities
{
    public class ProblemVersionEntity : BaseEntity, ICreatable
    {
        public Guid ProblemId { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public Guid CreatedBy { get; set; }
        public bool IsDraft { get; set; } = true;
        public bool IsPublished { get; set; } = false;
        public int Version { get; set; }
        public string Statement { get; set; } = "Sample problem statement";
        ICollection<TestCaseEntity> TestCases { get; set; } = new List<TestCaseEntity>();
        ICollection<AdditionalDefinitionEntity> AdditionalDefinitions { get; set; } = new List<AdditionalDefinitionEntity>();
        public ProblemEntity? Problem { get; set; }
    }
}
