using ServerAPIApp.Domain.Entities;

namespace ServerAPIApp.Contracts.DTOs
{
    public record ShortSubmissionDto(Guid Id, string SolutionLanguage, int PassedTests, int TotalTests)
    {
        public static ShortSubmissionDto From(Guid id, string solutionLanguage, int passedTests, int totalTests)
        {
            return new ShortSubmissionDto(id, solutionLanguage, passedTests, totalTests);
        }

        public static ShortSubmissionDto From(SubmissionEntity entity)
        {
            return new ShortSubmissionDto(entity.Id, entity.SolutionLanguage, entity.PassedTests, entity.TotalTests);
        }
    }
}
