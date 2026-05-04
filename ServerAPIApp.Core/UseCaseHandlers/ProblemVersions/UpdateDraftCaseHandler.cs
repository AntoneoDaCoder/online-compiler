using MediatR;
using Microsoft.Extensions.Options;
using ServerAPIApp.Contracts.Abstractions;
using ServerAPIApp.Core.UseCases.ProblemVersions;
using ServerAPIApp.DAL.Confs;
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

        private readonly string _bucketName;

        public UpdateDraftCaseHandler(IProblemVersionRepository repo, IObjectStorage storage, IOptions<MinioConfiguration> conf)
        {
            _repo = repo;
            _storage = storage;
            _bucketName = conf.Value.BucketName;
        }

        public async Task Handle(UpdateVersionDraftCase command, CancellationToken cancellationToken)
        {
            var entity = (await _repo.GetFilteredAsync(x => x.Id == command.VersionId, cancellationToken)).FirstOrDefault();

            if (entity is null)
                throw new ResourceNotFoundException("Resource not found");

            entity.Statement = command.Statement;
            entity.TotalTests = command.TotalTests;

            if (command.TestManifest is not null)
            {
                var key = $"problems/{command.ProblemId}/versions/{command.VersionId}/template.json";

                await _storage.DeleteObjectAsync(_bucketName, key, cancellationToken);

                var manifestJson = JsonSerializer.Serialize(command.TestManifest, _opts);

                if (!await _storage.UploadStringAsync(_bucketName, key, manifestJson, cancellationToken: cancellationToken))
                    throw new ObjectStorageUploadException("Failed to save tests.");

                entity.TestTemplateKey = key;
            }

            await _repo.UpdateAsync(entity, cancellationToken);
        }
    }
}
