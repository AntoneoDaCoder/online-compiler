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
        public long WallTimeMs { get; set; } = 0L;
        public long CpuTimeUs { get; set; } = 0L;
        public long PeakMemoryBytes { get; set; } = 0L;
        public required DateTimeOffset RequestSentAt { get; set; }
        public required DateTimeOffset ResponseSentAt { get; set; }
        public double LatencyInSeconds => (ResponseSentAt - RequestSentAt).TotalSeconds;
    }
}
