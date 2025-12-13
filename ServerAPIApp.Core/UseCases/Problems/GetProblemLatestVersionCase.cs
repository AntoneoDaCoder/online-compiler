using MediatR;
using ServerAPIApp.Contracts.DTOs;

namespace ServerAPIApp.Core.UseCases.Problems
{
    public record GetProblemLatestVersionCase(string ProblemSlug) : IRequest<EditorProblemVersionDto>;
}
