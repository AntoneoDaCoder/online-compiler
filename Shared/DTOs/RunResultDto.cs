using Shared.DTOs.TestReports;
using Shared.Enums;
using System.Text.Json.Serialization;

namespace Shared.DTOs
{
    public sealed class RunResultDto
    {
        [JsonPropertyName("wallTimeMs")]
        public long WallTimeMs { get; set; }

        [JsonPropertyName("cpuTimeUs")]
        public long CpuTimeUs { get; set; }

        [JsonPropertyName("peakMemoryBytes")]
        public long PeakMemoryBytes { get; set; }

        [JsonPropertyName("testReport")]
        public TestRunReportDto? TestReport { get; set; }

        [JsonPropertyName("status")]
        public required ExecutionStatus Status { get; set; }

        [JsonPropertyName("state")]
        public required string State { get; set; } = string.Empty;
        [JsonPropertyName("stdOut")]
        public string? StdOut { get; set; }
        [JsonPropertyName("stdErr")]
        public string? StdErr { get; set; }
        [JsonPropertyName("exitCode")]
        public required int ExitCode { get; set; }
    }
}
