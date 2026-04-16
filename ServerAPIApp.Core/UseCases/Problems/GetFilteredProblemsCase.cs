using MediatR;
using ServerAPIApp.Contracts.DTOs.Problems;

namespace ServerAPIApp.Core.UseCases.Problems
{
    public record GetFilteredProblemsCase(IEnumerable<string> Roles) : IRequest<IEnumerable<ProblemDto>?>;
}
