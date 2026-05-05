using Shared.Enums;

namespace Shared.DTOs
{
    public sealed class ExecutionResultDto
    {
        public ExecutionStatus Status { get; set; }
        public int ExitCode { get; set; }
        public string? ConsoleOutput { get; set; }
        public int PassedTests { get; set; }
        public required int TotalTests { get; set; }
        public required DateTimeOffset RequestSentAt { get; set; }
        public required DateTimeOffset ResponseSentAt { get; set; }
        public double LatencyInSeconds => (ResponseSentAt - RequestSentAt).TotalSeconds;
    }
}
