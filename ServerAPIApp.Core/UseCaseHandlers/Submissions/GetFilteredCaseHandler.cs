using MediatR;
using ServerAPIApp.Contracts.Abstractions;
using ServerAPIApp.Core.UseCases.Submissions;
using ServerAPIApp.Domain.Entities;

namespace ServerAPIApp.Core.UseCaseHandlers.Submissions
{
    public class GetFilteredCaseHandler : IRequestHandler<GetFilteredUserSubmissionsCase, List<SubmissionEntity>?>
    {
        private ISubmissionRepository _repo;
        public GetFilteredCaseHandler(ISubmissionRepository repo)
        {
            _repo = repo;
        }

        public async Task<List<SubmissionEntity>?> Handle(GetFilteredUserSubmissionsCase command, CancellationToken cancellationToken)
        {
            return await _repo.GetUserSubmissionsFilteredByLanguageAsync(command.UserId, x => command.LanguageCodes.Contains(x.SolutionLanguage), cancellationToken);
        }
    }
}
