namespace Shared.Models
{
    public class Problem
    {
        public string Name { get; set; } = string.Empty;
        public string AdditionalDefinitions { get; set; } = string.Empty;
        public ICollection<TestCase> TestCases { get; set; }
    }
}
