using MediatR;
using ServerAPIApp.Contracts.DTOs;

namespace ServerAPIApp.Core.UseCases.Problems
{
    public record GetFilteredProblemsCase(IEnumerable<string> Roles) : IRequest<IEnumerable<ProblemDto>?>;
}
