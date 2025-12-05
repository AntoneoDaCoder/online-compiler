using MediatR;
using ServerAPIApp.Contracts.Abstractions;
using ServerAPIApp.Core.Helpers;
using ServerAPIApp.Core.UseCases.ProblemVersions;
using ServerAPIApp.Domain.Entities;
using ServerAPIApp.Domain.Exceptions;
using System.Text.Json;

namespace ServerAPIApp.Core.UseCaseHandlers.ProblemVersions
{
    public class CreateDraftCaseHandler : IRequestHandler<CreateVersionDraftCase, ProblemVersionEntity>
    {
        private IProblemVersionRepository _repo;
        private IObjectStorage _storage;

        //TODO: move this to config as well
        const string _bucketName = "xdd";

        public CreateDraftCaseHandler(IProblemVersionRepository repo, IObjectStorage storage)
        {
            _repo = repo;
            _storage = storage;
        }

        public async Task<ProblemVersionEntity> Handle(CreateVersionDraftCase command, CancellationToken cancellationToken)
        {
            var (draft, manifestJson) = command.ToEntity();

            if (manifestJson is not null)
            {
                var key = $"problems/{draft.ProblemId}/versions/{draft.Id}/template.json";

                if (!await _storage.UploadStringAsync(_bucketName, key, manifestJson, cancellationToken: cancellationToken))
                    throw new ObjectStorageUploadException("Failed to save tests.");

                draft.TestTemplateKey = key;
            }

            return await _repo.CreateDraftAsync(draft, cancellationToken);
        }
    }
}
