using ServerAPIApp.Domain.Entities;

namespace ServerAPIApp.Contracts.DTOs
{
    public record SubmissionDto(Guid SubmissionId, Guid VersionId, string ProblemSlug, string SolutionLanguage, string Solution, int NumPassedTests, int TotalTests)
    {
        public static SubmissionDto From(Guid subId, Guid verId, string pSlug, string sLang, string solution, int numPassed, int numTotal)
        {
            return new SubmissionDto(subId, verId, pSlug, sLang, solution, numPassed, numTotal);
        }

        public static SubmissionDto From(SubmissionEntity entity)
        {
            return new SubmissionDto(entity.Id, entity.ProblemVersionId, entity.ProblemVersion.Problem.Slug, entity.SolutionLanguage, entity.Solution, entity.PassedTests, entity.TotalTests);
        }
    }
}
