using MediatR;
using ServerAPIApp.Contracts.Abstractions;
using ServerAPIApp.Core.UseCases.Submissions;

namespace ServerAPIApp.Core.UseCaseHandlers.Submissions
{
    public class DeleteCaseHandler : IRequestHandler<DeleteSubmissionCase>
    {
        private ISubmissionRepository _repo;

        public DeleteCaseHandler(ISubmissionRepository repo)
        {
            _repo = repo;
        }

        public async Task Handle(DeleteSubmissionCase command, CancellationToken cancellationToken)
        {
            await _repo.DeleteAsync(command.Id, cancellationToken);
        }
    }
}
