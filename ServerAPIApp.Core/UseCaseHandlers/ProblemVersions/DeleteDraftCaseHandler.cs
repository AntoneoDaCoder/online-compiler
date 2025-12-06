using MediatR;
using ServerAPIApp.Contracts.Abstractions;
using ServerAPIApp.Core.UseCases.ProblemVersions;

namespace ServerAPIApp.Core.UseCaseHandlers.ProblemVersions
{
    public class DeleteDraftCaseHandler : IRequestHandler<DeleteVersionDraftCase>
    {
        private IProblemVersionRepository _repo;
        private IObjectStorage _storage;

        //TODO: move this to config as well
        const string _bucketName = "xdd";

        public DeleteDraftCaseHandler(IProblemVersionRepository repo, IObjectStorage storage)
        {
            _repo = repo;
            _storage = storage;
        }

        public async Task Handle(DeleteVersionDraftCase command, CancellationToken cancellationToken)
        {
            var key = $"problems/{command.ProblemId}/versions/{command.Id}/template.json";

            await _storage.DeleteObjectAsync(_bucketName, key, cancellationToken);

            await _repo.DeleteDraftAsync(command.Id, cancellationToken);
        }
    }
}
