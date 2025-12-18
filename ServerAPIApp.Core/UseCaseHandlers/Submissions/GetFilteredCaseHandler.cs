using MediatR;
using ServerAPIApp.Contracts.Abstractions;
using ServerAPIApp.Contracts.DTOs;
using ServerAPIApp.Core.Helpers;
using ServerAPIApp.Core.UseCases.Submissions;
using ServerAPIApp.Domain.Entities;
using System.Linq.Expressions;

namespace ServerAPIApp.Core.UseCaseHandlers.Submissions
{
    public class GetFilteredCaseHandler : IRequestHandler<GetFilteredUserSubmissionsCase, IEnumerable<ShortSubmissionDto>?>
    {
        private ISubmissionRepository _repo;
        public GetFilteredCaseHandler(ISubmissionRepository repo)
        {
            _repo = repo;
        }

        public async Task<IEnumerable<ShortSubmissionDto>?> Handle(GetFilteredUserSubmissionsCase command, CancellationToken cancellationToken)
        {
            var filter = BuildFilter(command.UserId, command.LanguageCodes, command.IsSuccessful);

            var submissions = await _repo.GetFilteredUserSubmissionsAsync(filter, cancellationToken);

            return submissions.ToDto();
        }

        private static Expression<Func<SubmissionEntity, bool>> BuildFilter(Guid id, IEnumerable<string>? languages, bool? isSuccessful)
        {
            Expression<Func<SubmissionEntity, bool>> filter = entity => entity.CreatedBy == id;

            if (languages is not null && languages.Any())
            {
                filter = filter.And(entity => languages.Contains(entity.SolutionLanguage));
            }

            if (isSuccessful.HasValue)
            {
                if (isSuccessful.Value)
                {
                    filter = filter.And(entity => entity.PassedTests == entity.TotalTests);
                }
                else
                {
                    filter = filter.And(entity => entity.PassedTests != entity.TotalTests);
                }
            }

            return filter;
        }
    }
}
