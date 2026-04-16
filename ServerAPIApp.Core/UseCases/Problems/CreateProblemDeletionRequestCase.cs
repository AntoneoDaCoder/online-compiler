using ServerAPIApp.Core.Abstractions;

namespace ServerAPIApp.Core.UseCases.Problems
{
    public record CreateProblemDeletionRequestCase(Guid ProblemId, Guid InitiatorId, string Reason) : IValidatableRequest<Guid>;
}
