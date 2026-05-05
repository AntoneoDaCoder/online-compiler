using System.Text.Json.Serialization;

namespace Shared.DTOs.TestReports
{
    public sealed class TestRunReportDto
    {
        [JsonPropertyName("totalTests")]
        public int TotalTests { get; set; }
        [JsonPropertyName("passedTests")]
        public int PassedTests { get; set; }
        [JsonPropertyName("failedTests")]
        public FailedTestDto[] FailedTests { get; set; } = Array.Empty<FailedTestDto>();
    }
}
