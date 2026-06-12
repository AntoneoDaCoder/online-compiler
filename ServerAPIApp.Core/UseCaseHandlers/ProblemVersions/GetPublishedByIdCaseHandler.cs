using MediatR;
using Microsoft.EntityFrameworkCore;
using ServerAPIApp.Contracts.Abstractions;
using ServerAPIApp.Contracts.DTOs.Problems;
using ServerAPIApp.Core.UseCases.ProblemVersions;

namespace ServerAPIApp.Core.UseCaseHandlers.ProblemVersions
{
    public class GetPublishedByIdCaseHandler : IRequestHandler<GetPublishedByIdCase, UserProblemVersionDto?>
    {
        private IProblemVersionRepository _repo;

        public GetPublishedByIdCaseHandler(IProblemVersionRepository repo)
        {
            _repo = repo;
        }

        public async Task<UserProblemVersionDto?> Handle(GetPublishedByIdCase command, CancellationToken cancellationToken)
        {
            var entity = await _repo.Query()
              .AsNoTracking()
              .Where(x => x.Id == command.VersionId && x.IsPublished)
              .Include(x => x.SupportedLanguages)
              .ThenInclude(x => x.Language)
              .FirstOrDefaultAsync(cancellationToken);

            return UserProblemVersionDto.From(entity);
        }
    }
}
