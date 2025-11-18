using System.Text.Json;

namespace ServerAPIApp.Contracts.DTOs.ManifestHelpers
{
    public class SampleTest
    {
        public string Name { get; set; } = "";
        public JsonElement? Inputs { get; set; }
        public JsonElement? Expected { get; set; }
        public string Comparator { get; set; } = "eq";
    }
}
