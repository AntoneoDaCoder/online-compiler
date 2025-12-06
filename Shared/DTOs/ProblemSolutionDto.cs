namespace Shared.DTOs
{
    public class ProblemSolutionDto
    {
        public Guid RequestId { get; set; }
        public Guid VersionId { get; set; }
        public Guid UserId { get; set; }
        public string TestManifestJson { get; set; }
        public string LanguageCode { get; set; } = string.Empty;
        public string UserSolution { get; set; } = string.Empty;
        public DateTimeOffset SentAt { get; set; }
    }
}
