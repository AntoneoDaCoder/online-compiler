namespace Shared.Models
{
    public class Problem
    {
        public string Name { get; set; } = string.Empty;
        public ICollection<AdditionalDefinition> AdditionalDefinitions { get; set; } = new List<AdditionalDefinition>();
        public ICollection<TestCase> TestCases { get; set; } = new List<TestCase>();
    }
}
