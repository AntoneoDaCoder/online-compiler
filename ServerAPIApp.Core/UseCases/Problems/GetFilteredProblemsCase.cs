using MediatR;
using ServerAPIApp.Contracts.DTOs.Problems;

namespace ServerAPIApp.Core.UseCases.Problems
{
    public record GetFilteredProblemsCase(IEnumerable<string> Roles, bool? GetDeleted = null, bool? IncludeLatestVersion = null,
        bool? IncludeLanguages = null) : IRequest<IEnumerable<ProblemDto>?>;
}
