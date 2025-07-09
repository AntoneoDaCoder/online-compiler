using Shared.Enums;

namespace Shared.DTOs
{
    public sealed class CodeRequestDto
    {
        public Guid RequestId { get; set; } = Guid.NewGuid();
        public string Code { get; set; } = string.Empty;
        public long MaxAllowedTimeInMilliseconds { get; set; }
        public string CallbackUrl { get; set; } = string.Empty;
        public DateTime RequestSentAt { get; set; }
    }
}
