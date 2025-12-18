using MediatR;
using ServerAPIApp.Contracts.Abstractions;
using ServerAPIApp.Contracts.DTOs;
using ServerAPIApp.Core.UseCases.ProblemVersions;
using ServerAPIApp.Domain.Exceptions.NotFoundExceptions;
using Shared.DTOs;
using Shared.Helpers;

namespace ServerAPIApp.Core.UseCaseHandlers.ProblemVersions
{
    public class GetEditorVersionCaseHandler : IRequestHandler<GetEditorVersionCase, EditorProblemVersionDto?>
    {
        private IProblemVersionRepository _repo;
        private IObjectStorage _storage;

        private const string _bucketName = "manifestbucket";

        public GetEditorVersionCaseHandler(IProblemVersionRepository repo, IObjectStorage storage)
        {
            _repo = repo;
            _storage = storage;
        }

        public async Task<EditorProblemVersionDto?> Handle(GetEditorVersionCase command, CancellationToken cancellationToken)
        {
            var version = await _repo.GetByIdWithLanguagesAsync(command.VersionId, cancellationToken);

            if (version is null)
                throw new ResourceNotFoundException("Such version doesn't exist");

            var key = $"problems/{version.ProblemId}/versions/{version.Id}/template.json";

            var manifestString = await _storage.GetStringAsync(_bucketName, key, cancellationToken);

            ManifestDto? manifest = null;

            if (manifestString != null)
                manifest = ManifestParser.Parse(manifestString);

            return EditorProblemVersionDto.From(version, manifest);
        }
    }
}
