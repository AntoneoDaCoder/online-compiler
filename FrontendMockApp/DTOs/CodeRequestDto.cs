using FrontendMockApp.Enums;

namespace FrontendMockApp.DTOs
{
    public sealed class CodeRequestDto
    {
        public Guid RequestId { get; set; } = Guid.NewGuid();
        public string Code { get; set; } = string.Empty;
        public long MaxAllowedTimeInMilliseconds { get; set; }
        public string? CallbackUrl { get; set; }
        public DateTime RequestSentAt { get; set; }
    }
}
