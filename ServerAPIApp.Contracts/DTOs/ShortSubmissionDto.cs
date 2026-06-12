using ServerAPIApp.Domain.Entities;

namespace ServerAPIApp.Contracts.DTOs
{
    public record ShortSubmissionDto(Guid Id, string SolutionLanguage, int PassedTests, int TotalTests, DateTimeOffset Created)
    {
        public static ShortSubmissionDto From(Guid id, string solutionLanguage, int passedTests, int totalTests, DateTimeOffset created)
        {
            return new ShortSubmissionDto(id, solutionLanguage, passedTests, totalTests, created);
        }

        public static ShortSubmissionDto From(SubmissionEntity entity)
        {
            return new ShortSubmissionDto(entity.Id, entity.SolutionLanguage, entity.PassedTests, entity.TotalTests,entity.CreatedAt!.Value);
        }
    }
}
