using MediatR;

namespace ServerAPIApp.Core.UseCases.Problems
{
    public record ApproveProblemDeletionRequestCase(Guid RequestId) : IRequest<Guid>;
}
