using System.Text.Json.Serialization;

namespace Shared.DTOs.TestReports
{
    public sealed class FailedTestDto
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
        [JsonPropertyName("reason")]
        public string Reason { get; set; } = string.Empty;
    }
}
