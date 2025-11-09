namespace ServerAPIApp.Domain.Entities
{
    public class SubmissionEntity : BaseEntity, ICreatable
    {
        public Guid ProblemVersionId { get; set; }
        public ProblemVersionEntity? ProblemVersion { get; set; }


        public DateTimeOffset CreatedAt { get; set; }
        public Guid CreatedBy { get; set; } //index
        public UserEntity? Creator { get; set; }


        public string Solution { get; set; } = "Sample solution";
        public int PassedTests { get; set; }
        public int TotalTests { get; set; }
        public string SolutionLanguage { get; set; } = "Sample language";
    }
}
