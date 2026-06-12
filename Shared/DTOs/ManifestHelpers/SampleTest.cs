using System.Text.Json;

namespace Shared.DTOs.ManifestHelpers
{
    public class SampleTest
    {
        public string Name { get; set; } = "";
        public long TimeoutMs { get; set; }
        public JsonElement? Inputs { get; set; }
        public JsonElement? Expected { get; set; }
        public string Comparator { get; set; } = "eq";
    }
}
