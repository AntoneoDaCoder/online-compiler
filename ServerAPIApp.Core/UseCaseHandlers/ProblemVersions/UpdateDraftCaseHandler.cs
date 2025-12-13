using MediatR;
using ServerAPIApp.Contracts.Abstractions;
using ServerAPIApp.Core.Helpers;
using ServerAPIApp.Core.UseCases.ProblemVersions;
using ServerAPIApp.Domain.Exceptions.InternalServerExceptions;
using ServerAPIApp.Domain.Exceptions.NotFoundExceptions;
using System.Text.Json;

namespace ServerAPIApp.Core.UseCaseHandlers.ProblemVersions
{
    public class UpdateDraftCaseHandler : IRequestHandler<UpdateVersionDraftCase>
    {
        private IObjectStorage _storage;
        private IProblemVersionRepository _repo;

        private static readonly JsonSerializerOptions _opts = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        };

        //TODO: move this to config as well
        const string _bucketName = "xdd";

        public UpdateDraftCaseHandler(IProblemVersionRepository repo, IObjectStorage storage)
        {
            _repo = repo;
            _storage = storage;
        }

        public async Task Handle(UpdateVersionDraftCase command, CancellationToken cancellationToken)
        {
            var (entity, manifest) = command.ToEntity();

            if (manifest is not null)
            {
                var key = $"problems/{command.ProblemId}/versions/{command.VersionId}/template.json";

                await _storage.DeleteObjectAsync(_bucketName, key, cancellationToken);

                var manifestJson = JsonSerializer.Serialize(manifest, _opts);

                if (!await _storage.UploadStringAsync(_bucketName, key, manifestJson, cancellationToken: cancellationToken))
                    throw new ObjectStorageUploadException("Failed to save tests.");

                entity.TestTemplateKey = key;
            }

            var updated = await _repo.UpdateDraftAsync(entity, cancellationToken);

            if (!updated)
                throw new ResourceNotFoundException("Resource not found");
        }
    }
}
