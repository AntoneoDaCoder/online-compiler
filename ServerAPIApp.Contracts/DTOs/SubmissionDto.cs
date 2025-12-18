using ServerAPIApp.Domain.Entities;

namespace ServerAPIApp.Contracts.DTOs
{
    public record SubmissionDto(Guid SubmissionId, Guid VersionId, string ProblemSlug, string Title, string SolutionLanguage, string Solution, int NumPassedTests, int TotalTests)
    {
        public static SubmissionDto From(Guid subId, Guid verId, string pSlug, string title, string sLang, string solution, int numPassed, int numTotal)
        {
            return new SubmissionDto(subId, verId, pSlug, title, sLang, solution, numPassed, numTotal);
        }

        public static SubmissionDto From(SubmissionEntity entity)
        {
            return new SubmissionDto(entity.Id, entity.ProblemVersionId, entity.ProblemVersion.Problem.Slug,
                entity.ProblemVersion.Problem.Title, entity.SolutionLanguage,
                entity.Solution, entity.PassedTests, entity.TotalTests);
        }
    }
}
