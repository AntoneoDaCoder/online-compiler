using MediatR;
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
            var entity = await _repo.GetByIdWithLanguagesAsync(command.VersionId, cancellationToken);

            if (entity is not null && !entity.IsPublished)
                return null;

            return UserProblemVersionDto.From(entity);
        }
    }
}
