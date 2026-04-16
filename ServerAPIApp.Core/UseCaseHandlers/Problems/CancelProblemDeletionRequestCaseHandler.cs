using MediatR;
using ServerAPIApp.Contracts.Abstractions;
using ServerAPIApp.Core.UseCases.Problems;
using ServerAPIApp.Domain.Entities;

namespace ServerAPIApp.Core.UseCaseHandlers.Problems
{
    public class CancelProblemDeletionRequestCaseHandler : IRequestHandler<CancelProblemDeletionRequestCase>
    {
        private readonly IProblemDeletionRequestRepository _repo;

        public CancelProblemDeletionRequestCaseHandler(IProblemDeletionRequestRepository repo)
        {
            _repo = repo;
        }

        public async Task Handle(CancelProblemDeletionRequestCase request, CancellationToken cancellationToken)
        {
            var stub = new ProblemDeletionRequestEntity()
            {
                Id = request.RequestId
            };

            await _repo.DeleteAsync(stub, cancellationToken);
        }
    }
}
