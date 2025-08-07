using Shared.Models;

namespace Shared.DTOs
{
    public class ProblemSolutionDto
    {
        public Guid RequestId { get; set; }
        public long MaxAllowedTimeInMilliseconds { get; set; }
        public string Code { get; set; } = string.Empty;
        public Problem Problem { get; set; }
        public string CallbackUrl { get; set; } = string.Empty;
        public DateTime SentAt { get; set; }
    }
}
