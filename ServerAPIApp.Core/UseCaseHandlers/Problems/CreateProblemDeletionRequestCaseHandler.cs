using MediatR;
using ServerAPIApp.Contracts.Abstractions;
using ServerAPIApp.Contracts.DTOs.Problems;
using ServerAPIApp.Core.UseCases.Problems;
using ServerAPIApp.Domain.Entities;

namespace ServerAPIApp.Core.UseCaseHandlers.Problems
{
    public class CreateProblemDeletionRequestCaseHandler : IRequestHandler<CreateProblemDeletionRequestCase, ProblemDeletionRequestDto>
    {
        private readonly IProblemDeletionRequestRepository _repo;
        private readonly IProblemRepository _problemRepository;

        public CreateProblemDeletionRequestCaseHandler(IProblemDeletionRequestRepository repo, IProblemRepository problemRepo)
        {
            _repo = repo;
            _problemRepository = problemRepo;
        }

        public async Task<ProblemDeletionRequestDto> Handle(CreateProblemDeletionRequestCase command, CancellationToken cancellationToken)
        {
            var request = new ProblemDeletionRequestEntity()
            {
                Id = Guid.NewGuid(),
                InitiatorId = command.InitiatorId,
                ProblemId = command.ProblemId,
                Reason = command.Reason,
                IsApproved = false
            };

            var res = await _repo.CreateAsync(request, cancellationToken);

            //if problem doesn't exist the insert operation above fill fail
            var linkedProblem = (await _problemRepository.GetFilteredAsync(x => x.Id == command.ProblemId, cancellationToken)).FirstOrDefault();
            res.Problem = linkedProblem!;

            return ProblemDeletionRequestDto.From(res)!;
        }
    }
}
