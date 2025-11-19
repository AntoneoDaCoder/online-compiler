using MediatR;

namespace ServerAPIApp.Core.UseCases.Problems
{
    public record DeleteProblemCase(Guid ProblemId, Guid InitiatorId) : IRequest;
}
