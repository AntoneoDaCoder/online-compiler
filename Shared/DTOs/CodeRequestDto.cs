namespace Shared.DTOs
{
    public sealed class CodeRequestDto
    {
        public Guid RequestId { get; set; } = Guid.NewGuid();
        public string Language {  get; set; } = string.Empty;
        public string ProblemName { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public long MaxAllowedTimeInMilliseconds { get; set; }
        public DateTime RequestSentAt { get; set; }
    }
}
