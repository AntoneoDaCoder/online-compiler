using ServerAPIApp.Contracts.DTOs.Problems;
using ServerAPIApp.Core.Abstractions;

namespace ServerAPIApp.Core.UseCases.Problems
{
    public record CreateProblemCase(Guid CreatorId, string Title, string Slug) : IValidatableRequest<ProblemDto>;
}
