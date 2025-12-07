using Shared.DTOs.ManifestHelpers;

namespace Shared.DTOs
{
    public class ManifestDto
    {
        public string Entrypoint { get; set; } = "";
        public Signature Signature { get; set; } = new Signature();
        public List<HelpersBlock> Helpers { get; set; } = new List<HelpersBlock>();
        public List<AdvancedTest> AdvancedTests { get; set; } = new();
        public List<SampleTest> SampleTests { get; set; } = new();
    }
}
