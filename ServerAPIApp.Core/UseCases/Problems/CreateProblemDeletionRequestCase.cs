using ServerAPIApp.Core.Abstractions;
using MediatR;

namespace ServerAPIApp.Core.UseCases.Problems
{
    public record CreateProblemDeletionRequestCase(Guid ProblemId, Guid InitiatorId, string Reason) : IValidatableRequest<Unit>;
}
