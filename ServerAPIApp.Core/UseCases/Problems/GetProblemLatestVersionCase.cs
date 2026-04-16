using MediatR;
using ServerAPIApp.Contracts.DTOs.Problems;

namespace ServerAPIApp.Core.UseCases.Problems
{
    public record GetProblemLatestVersionCase(string ProblemSlug) : IRequest<EditorProblemVersionDto>;
}
