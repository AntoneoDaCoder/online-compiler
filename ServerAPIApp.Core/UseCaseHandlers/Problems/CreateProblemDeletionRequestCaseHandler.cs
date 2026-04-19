using MediatR;
using ServerAPIApp.Contracts.Abstractions;
using ServerAPIApp.Core.UseCases.Problems;
using ServerAPIApp.Domain.Entities;

namespace ServerAPIApp.Core.UseCaseHandlers.Problems
{
    public class CreateProblemDeletionRequestCaseHandler : IRequestHandler<CreateProblemDeletionRequestCase, Unit>
    {
        private readonly IProblemDeletionRequestRepository _repo;

        public CreateProblemDeletionRequestCaseHandler(IProblemDeletionRequestRepository repo)
        {
            _repo = repo;
        }

        public async Task<Unit> Handle(CreateProblemDeletionRequestCase command, CancellationToken cancellationToken)
        {
            var request = new ProblemDeletionRequestEntity()
            {
                Id = Guid.NewGuid(),
                InitiatorId = command.InitiatorId,
                ProblemId = command.ProblemId,
                Reason = command.Reason,
            };

            await _repo.CreateAsync(request, cancellationToken);

            return Unit.Value;
        }
    }
}
