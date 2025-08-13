namespace Shared.Models
{
    public class TestCase
    {
        public string Name { get; set; } = "Test";
        public string TestLanguage { get; set; } = string.Empty;
        public string TestInitialization { get; set; } = string.Empty;
        public string InputExpression { get; set; } = string.Empty;
        public string OutputExpression { get; set; } = string.Empty;

        public TestCase()
        {
        }

        public TestCase(TestCase source)
        {
            Name = source.Name;
            TestLanguage = source.TestLanguage;
            TestInitialization = source.TestInitialization;
            InputExpression = source.InputExpression;
            OutputExpression = source.OutputExpression;
        }
    }
}
