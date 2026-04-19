using MediatR;
using ServerAPIApp.Contracts.DTOs.Problems;

namespace ServerAPIApp.Core.UseCases.Problems
{
    public record RestoreProblemCase(Guid ProblemId) : IRequest<ProblemDto>;
}
