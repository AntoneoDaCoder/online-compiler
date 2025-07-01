using ServerAPIApp.Core.Enums;

namespace ServerAPIApp.Core.DTOs
{
    public sealed class ExecutionResultDto
    {
        public ExecutionStatus Status { get; set; }
        public int ExitCode { get; set; }
        public string? ConsoleOutput { get; set; }
        public DateTime RequestSentAt { get; set; }
        public DateTime ResponseSentAt { get; set; }
        public double LatencyInSeconds => (ResponseSentAt - RequestSentAt).TotalSeconds;
    }
}
