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
            var filter = BuildFilter(request.Roles);

            var result = await _repo
                .Query()
                .Where(filter)
                .Include(x => x.LastPublishedVersion)
                .ThenInclude(x => x.SupportedLanguages)
                .ToListAsync(cancellationToken);

            return result.ToDto();
        }

        private static Expression<Func<ProblemEntity, bool>> BuildFilter(IEnumerable<string> roles)
        {
            Expression<Func<ProblemEntity, bool>> filter = x => x.LastPublishedVersionId != null && !x.IsDeleted;

            if (roles.Any(x => _editorRoles.Contains(x)))
            {
                filter = x => !x.IsDeleted;
            }

            return filter;
        }
    }
}
