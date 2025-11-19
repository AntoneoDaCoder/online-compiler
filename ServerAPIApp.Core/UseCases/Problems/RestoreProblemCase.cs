using MediatR;

namespace ServerAPIApp.Core.UseCases.Problems
{
    public record RestoreProblemCase(Guid ProblemId, Guid InitiatorId) : IRequest;
}
