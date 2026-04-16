using MediatR;

namespace ServerAPIApp.Core.UseCases.Problems
{
    public record CancelProblemDeletionRequestCase(Guid RequestId) : IRequest;
}
