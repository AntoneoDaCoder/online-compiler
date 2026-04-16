using MediatR;
using Microsoft.AspNetCore.Identity.Data;
using ServerAPIApp.Contracts.Abstractions;
using ServerAPIApp.Contracts.DTOs.Problems;
using ServerAPIApp.Core.Helpers;
using ServerAPIApp.Core.UseCases.Problems;
using ServerAPIApp.Domain.Entities;
using System.Linq.Expressions;


namespace ServerAPIApp.Core.UseCaseHandlers.Problems
{
    public class GetFilteredProblemsCaseHandler : IRequestHandler<GetFilteredProblemsCase, IEnumerable<ProblemDto>?>
    {
        //TODO: later change it
        private static readonly IEnumerable<string> _editorRoles = ["Admin", "Editor"];

        private IProblemRepository _repo;

        public GetFilteredProblemsCaseHandler(IProblemRepository repo)
        {
            _repo = repo;
        }

        public async Task<IEnumerable<ProblemDto>?> Handle(GetFilteredProblemsCase request, CancellationToken cancellationToken)
        {
            var filter = BuildFilter(request.Roles);

            var result = await _repo.GetFilteredWithLatestVersionsAsync(filter, cancellationToken);

            return result.ToDto();
        }

        private static Expression<Func<ProblemEntity, bool>> BuildFilter(IEnumerable<string> roles)
        {
            Expression<Func<ProblemEntity, bool>> filter = x => x.LastPublishedVersionId != null && !x.IsDeleted;

            if (roles.Any(x => _editorRoles.Contains(x)))
            {
                filter = x => true;
            }

            return filter;
        }
    }
}
