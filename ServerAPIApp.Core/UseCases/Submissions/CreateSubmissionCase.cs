using MediatR;

namespace ServerAPIApp.Core.UseCases.Submissions
{
    public record UpdateProblemCase(Guid VersionId,Guid CreatedBy,string Solution,int PassedTests, int TotalTests, string SolutionLanguage) : IRequest;
}
