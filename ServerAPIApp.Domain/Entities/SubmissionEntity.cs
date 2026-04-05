namespace ServerAPIApp.Domain.Entities
{
    public class SubmissionEntity : BaseEntity, ICreatable
    {
        public Guid ProblemVersionId { get; set; } = Guid.Empty;
        public ProblemVersionEntity? ProblemVersion { get; set; }


        public DateTimeOffset? CreatedAt { get; set; } = DateTimeOffset.MinValue;
        public Guid? CreatedBy { get; set; } = Guid.Empty;//index
        public UserEntity? Creator { get; set; }


        public string Solution { get; set; } = "Sample solution";
        public int PassedTests { get; set; }
        public int TotalTests { get; set; }
        //why not language entity lol????
        public string SolutionLanguage { get; set; } = "Sample language";
    }
}
