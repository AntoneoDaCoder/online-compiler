using ServerAPIApp.Contracts.DTOs.ManifestHelpers;

namespace ServerAPIApp.Contracts.DTOs
{
    public class ManifestDto
    {
        public string Entrypoint { get; set; } = "";
        public Signature Signature { get; set; } = new Signature();
        public HelpersBlock Helpers { get; set; } = new HelpersBlock();
        public List<AdvancedTest> AdvancedTests { get; set; } = new();
        public List<SampleTest> SampleTests { get; set; } = new();
    }
}
