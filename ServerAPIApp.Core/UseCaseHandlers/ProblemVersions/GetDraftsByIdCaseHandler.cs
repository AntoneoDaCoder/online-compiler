using MediatR;
using ServerAPIApp.Contracts.Abstractions;
using ServerAPIApp.Contracts.DTOs;
using ServerAPIApp.Core.Helpers;
using ServerAPIApp.Core.UseCases.ProblemVersions;

namespace ServerAPIApp.Core.UseCaseHandlers.ProblemVersions
{
    public class GetDraftsByIdCaseHandler : IRequestHandler<GetVersionDraftsByIdCase, DraftListDto>
    {
        private IProblemVersionRepository _repo;

        public GetDraftsByIdCaseHandler(IProblemVersionRepository repo)
        {
            _repo = repo;
        }

        public async Task<DraftListDto> Handle(GetVersionDraftsByIdCase command, CancellationToken cancellationToken)
        {
            var entities = await _repo.GetFilteredAsync(command.ProblemId, cancellationToken: cancellationToken);

            return DraftListDto.From(entities);
        }
    }
}
