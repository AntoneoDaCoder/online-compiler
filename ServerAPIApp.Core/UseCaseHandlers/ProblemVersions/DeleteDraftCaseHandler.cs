using MediatR;
using Microsoft.Extensions.Options;
using ServerAPIApp.Contracts.Abstractions;
using ServerAPIApp.Core.UseCases.ProblemVersions;
using ServerAPIApp.DAL.Confs;
using ServerAPIApp.Domain.Entities;

namespace ServerAPIApp.Core.UseCaseHandlers.ProblemVersions
{
    public class DeleteDraftCaseHandler : IRequestHandler<DeleteVersionDraftCase>
    {
        private IProblemVersionRepository _repo;
        private IObjectStorage _storage;

        private readonly string _bucketName;

        public DeleteDraftCaseHandler(IProblemVersionRepository repo, IObjectStorage storage, IOptions<MinioConfiguration> conf)
        {
            _repo = repo;
            _storage = storage;
            _bucketName = conf.Value.BucketName;
        }

        public async Task Handle(DeleteVersionDraftCase command, CancellationToken cancellationToken)
        {
            var key = $"problems/{command.ProblemId}/versions/{command.Id}/template.json";

            await _storage.DeleteObjectAsync(_bucketName, key, cancellationToken);

            var entityStub = new ProblemVersionEntity() { Id = command.Id };

            await _repo.DeleteAsync(entityStub, cancellationToken);
        }
    }
}
