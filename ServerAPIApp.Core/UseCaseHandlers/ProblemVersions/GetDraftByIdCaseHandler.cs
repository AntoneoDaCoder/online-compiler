using MediatR;
using ServerAPIApp.Contracts.Abstractions;
using ServerAPIApp.Contracts.DTOs;
using ServerAPIApp.Core.Helpers;
using ServerAPIApp.Core.UseCases.ProblemVersions;
using ServerAPIApp.Domain.Exceptions;
using System.Text.Json;

namespace ServerAPIApp.Core.UseCaseHandlers.ProblemVersions
{
    public class GetDraftByIdCaseHandler : IRequestHandler<GetVersionDraftByIdCase, EditorProblemVersionDto>
    {
        private IProblemVersionRepository _repo;
        private IObjectStorage _storage;

        //TODO: move this to config as well
        const string _bucketName = "xdd";

        public GetDraftByIdCaseHandler(IProblemVersionRepository repo, IObjectStorage storage)
        {
            _repo = repo;
            _storage = storage;
        }

        public async Task<EditorProblemVersionDto> Handle(GetVersionDraftByIdCase command, CancellationToken cancellationToken)
        {
            var entity = await _repo.GetByIdAsync(command.VersionId, isDraft: true, cancellationToken);

            if (entity is null)
                throw new ResourceNotFoundException("Resource not found");

            var key = $"problems/{entity.ProblemId}/versions/{entity.Id}/template.json";

            ManifestDto? manifestDto = null;

            var manifestString = await _storage.GetStringAsync(_bucketName, key, cancellationToken);

            if (manifestString is not null)
                manifestDto = JsonSerializer.Deserialize<ManifestDto>(manifestString);

            return (entity, manifestDto).ToDto();
        }
    }
}
