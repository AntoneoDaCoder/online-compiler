using MediatR;
using ServerAPIApp.Contracts.Abstractions;
using ServerAPIApp.Core.UseCases.Submissions;
using ServerAPIApp.Domain.Entities;

namespace ServerAPIApp.Core.UseCaseHandlers.Submissions
{
    public class GetUserSubmissionsCaseHandler : IRequestHandler<GetUserSubmissionsCase, List<SubmissionEntity>?>
    {
        private ISubmissionRepository _repo;

        public GetUserSubmissionsCaseHandler(ISubmissionRepository repo)
        {
            _repo = repo;
        }

        public async Task<List<SubmissionEntity>?> Handle(GetUserSubmissionsCase command, CancellationToken cancellationToken)
        {
            return await _repo.GetUserSubmissionsAsync(command.UserId, cancellationToken);
        }
    }
}
