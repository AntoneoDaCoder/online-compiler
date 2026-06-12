namespace ServerAPIApp.Contracts.DTOs
{
    public sealed class CodeRequestDto
    {
        public Guid RequestId { get; set; } = Guid.NewGuid();
        public string ProblemSlug { get; set; } = string.Empty;
        public Guid ProblemVersionId { get; set; }
        public string LanguageCode { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public DateTime RequestSentAt { get; set; }
    }
}
