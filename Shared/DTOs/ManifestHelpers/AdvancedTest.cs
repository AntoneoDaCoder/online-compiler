namespace Shared.DTOs.ManifestHelpers
{
    public class AdvancedTest
    {
        public string Name { get; set; } = "";
        public bool IsAsync { get; set; } = true;
        public long TimeoutMs { get; set; }
        public string Source { get; set; } = ""; // тело метода
    }
}
