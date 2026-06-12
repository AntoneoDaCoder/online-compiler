using System.Text.Json.Serialization;

namespace Shared.DTOs
{
    public sealed class RunRequestDto
    {
        [JsonPropertyName("executorFileName")]
        public required string ExecutorFileName { get; set; } = string.Empty;
        [JsonPropertyName("commandLineArguments")]
        public ICollection<string> CommandLineArguments { get; set; } = new List<string>();
        [JsonPropertyName("executableFileName")]
        public required string ExecutableFileName { get; set; } = string.Empty;
        [JsonPropertyName("maxProcessLifetime")]
        public required int MaxProcessLifetime { get; set; }
    }
}
