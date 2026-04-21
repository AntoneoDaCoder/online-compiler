using MediatR;

namespace ServerAPIApp.Core.UseCases.Problems
{
    public record CancelProblemDeletionRequestCase(Guid SenderId, Guid RequestId, IEnumerable<string> SenderRoles) : IRequest<Guid?>;
}
