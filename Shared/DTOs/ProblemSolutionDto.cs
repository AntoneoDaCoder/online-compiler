using Shared.Models;

namespace Shared.DTOs
{
    public class ProblemSolutionDto
    {
        public Guid RequestId { get; set; }
        public long MaxAllowedTimeInMilliseconds { get; set; }
        public string Language { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public Problem Problem { get; set; }
        public DateTime SentAt { get; set; }
    }
}
