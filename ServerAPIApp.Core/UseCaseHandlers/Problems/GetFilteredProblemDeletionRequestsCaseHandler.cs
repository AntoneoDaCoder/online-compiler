using MediatR;
using ServerAPIApp.Contracts.Abstractions;
using ServerAPIApp.Core.Helpers;
using ServerAPIApp.Core.UseCases.Problems;
using ServerAPIApp.Contracts.DTOs.Problems;

namespace ServerAPIApp.Core.UseCaseHandlers.Problems
{
    public class GetFilteredProblemDeletionRequestsCaseHandler : IRequestHandler<GetFilteredProblemDeletionRequestsCase, IEnumerable<ProblemDeletionRequestDto>>
    {
        private readonly IProblemDeletionRequestRepository _repo;

        public GetFilteredProblemDeletionRequestsCaseHandler(IProblemDeletionRequestRepository repo)
        {
            _repo = repo;
        }

        public async Task<IEnumerable<ProblemDeletionRequestDto>> Handle(GetFilteredProblemDeletionRequestsCase request, CancellationToken cancellationToken)
        {
            var entities = await _repo.GetFilteredAsync(request.Filter, cancellationToken, includes: x => x.Problem);

            if (entities is null)
                entities = [];

            return entities.ToDto();
        }
    }
}
