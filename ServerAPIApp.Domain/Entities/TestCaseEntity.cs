//TODO: implement a universal template for tests

namespace ServerAPIApp.Domain.Entities
{
    public class TestCaseEntity : BaseEntity
    {
        public Guid ProblemVersionId { get; set; }
        public string Name { get; set; } = "Test";
        public string TestLanguage { get; set; } = string.Empty; //index for faster search
        public string TestInitialization { get; set; } = string.Empty;
        public string InputExpression { get; set; } = string.Empty;
        public string OutputExpression { get; set; } = string.Empty;
        public ProblemVersionEntity? ProblemVersion { get; set; }
        public int TimeLimitMs { get; set; }
    }
}
