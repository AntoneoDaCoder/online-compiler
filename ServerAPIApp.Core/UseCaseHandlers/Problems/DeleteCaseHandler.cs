using MediatR;
using ServerAPIApp.Contracts.Abstractions;
using ServerAPIApp.Core.UseCases.Problems;

namespace ServerAPIApp.Core.UseCaseHandlers.Problems
{
    public class DeleteCaseHandler : IRequestHandler<DeleteProblemCase>
    {
        private IProblemRepository _repo;
        public DeleteCaseHandler(IProblemRepository repo)
        {
            _repo = repo;
        }


        //TODO: remove TimeSpan stub, move it either to conf or to secrets
        public async Task Handle(DeleteProblemCase command, CancellationToken cancellationToken)
        {
            await _repo.SoftDeleteAsync(command.ProblemId, command.InitiatorId, TimeSpan.FromDays(14), cancellationToken);
        }
    }
}
