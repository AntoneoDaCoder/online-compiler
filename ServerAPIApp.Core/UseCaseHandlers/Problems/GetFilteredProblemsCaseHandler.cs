using MediatR;
using Microsoft.EntityFrameworkCore;
using ServerAPIApp.Contracts.Abstractions;
using ServerAPIApp.Contracts.DTOs.Problems;
using ServerAPIApp.Core.Helpers;
using ServerAPIApp.Core.UseCases.Problems;
using ServerAPIApp.Domain.Constants;
using ServerAPIApp.Domain.Entities;
using System.Linq.Expressions;


namespace ServerAPIApp.Core.UseCaseHandlers.Problems
{
    public class GetFilteredProblemsCaseHandler : IRequestHandler<GetFilteredProblemsCase, IEnumerable<ProblemDto>?>
    {
        private static readonly IEnumerable<string> _editorRoles = [UserRelatedConstants.AdminRoleName, UserRelatedConstants.EditorRoleName];

        private IProblemRepository _repo;

        public GetFilteredProblemsCaseHandler(IProblemRepository repo)
        {
            _repo = repo;
        }

        public async Task<IEnumerable<ProblemDto>?> Handle(GetFilteredProblemsCase request, CancellationToken cancellationToken)
        {
            var filter = BuildFilter(request);

            var query = _repo
                .Query()
                .AsNoTracking()
                .Where(filter);

            if (request.IncludeLatestVersion.HasValue && request.IncludeLatestVersion.Value
                && request.IncludeLanguages.HasValue && request.IncludeLanguages.Value)
            {
                query = query
                .Include(x => x.LastPublishedVersion)
                .ThenInclude(x => x.SupportedLanguages);
            }

            var result = await query.ToListAsync(cancellationToken);

            return result.ToDto();
        }

        private static Expression<Func<ProblemEntity, bool>> BuildFilter(GetFilteredProblemsCase request)
        {
            Expression<Func<ProblemEntity, bool>> filter = x => x.LastPublishedVersionId != null;

            if (request.Roles.Any(x => _editorRoles.Contains(x)))
            {
                filter = x => true;
            }

            if (request.GetDeleted.HasValue)
            {
                filter = filter.And(x => x.IsDeleted == request.GetDeleted.Value);
            }

            return filter;
        }
    }
}
