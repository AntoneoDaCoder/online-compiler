namespace ServerAPIApp.Domain.Entities
{
    public class SubmissionEntity : BaseEntity, ICreatable
    {
        public ProblemVersionEntity? ProblemVersion { get; set; }

        //public User? Creator {get;set;}
        public Guid ProblemVersionId { get; set; }
        public Guid CreatorId { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public string Solution { get; set; } = "Sample solution";
        public string Status { get; set; } = "Sample status";
        public string SolutionLanguage { get; set; } = "Sample lamguage";
    }
}
