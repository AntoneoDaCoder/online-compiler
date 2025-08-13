namespace Shared.Models
{
    public class Problem
    {
        public string Name { get; set; } = string.Empty;
        public ICollection<AdditionalDefinition> AdditionalDefinitions { get; set; } = new List<AdditionalDefinition>();
        public ICollection<TestCase> TestCases { get; set; } = new List<TestCase>();

        public Problem()
        { }

        public Problem(Problem source, string language)
        {
            Name = source.Name;

            AdditionalDefinitions = source.AdditionalDefinitions
                .Where(ad => ad.Language == language)
                .Select(ad => new AdditionalDefinition(ad))
                .ToList();

            TestCases = source.TestCases
                .Where(tc => tc.TestLanguage == language)
                .Select(tc => new TestCase(tc))
                .ToList();
        }
    }
}
