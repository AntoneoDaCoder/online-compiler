namespace ServerAPIApp.Contracts.DTOs.ManifestHelpers
{
    public class AdvancedTest
    {
        public string Name { get; set; } = "";
        public bool IsAsync { get; set; } = true;
        public int TimeoutMs { get; set; } = 2000;
        public string Source { get; set; } = ""; // тело метода
    }
}
