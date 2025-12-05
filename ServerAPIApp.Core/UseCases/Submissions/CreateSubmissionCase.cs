using MediatR;
using ServerAPIApp.Domain.Entities;

namespace ServerAPIApp.Core.UseCases.Submissions
{
    public record CreateSubmissionCase(Guid VersionId, Guid CreatedBy, string BriefStatus, string Solution, int PassedTests, int TotalTests, string SolutionLanguage) : IRequest<SubmissionEntity>;
}
