using ServerAPIApp.Core.Abstractions;
using MediatR;
using ServerAPIApp.Contracts.DTOs.Problems;

namespace ServerAPIApp.Core.UseCases.Problems
{
    public record CreateProblemDeletionRequestCase(Guid ProblemId, Guid InitiatorId, string Reason) : IValidatableRequest<ProblemDeletionRequestDto>;
}
