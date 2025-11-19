using MediatR;
using ServerAPIApp.Contracts.Abstractions;
using ServerAPIApp.Core.UseCases.Problems;

namespace ServerAPIApp.Core.UseCaseHandlers.Problems
{
    public class RestoreCaseHandler : IRequestHandler<RestoreProblemCase>
    {
        private IProblemRepository _repo;
        public RestoreCaseHandler(IProblemRepository repo)
        {
            _repo = repo;
        }

        public async Task Handle(RestoreProblemCase command, CancellationToken cancellationToken)
        {
            await _repo.CancelSoftDeleteAsync(command.ProblemId, command.InitiatorId, cancellationToken);
        }
    }
}
