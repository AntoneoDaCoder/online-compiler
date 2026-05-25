using MediatR;
using ServerAPIApp.Contracts.Abstractions;
using ServerAPIApp.Contracts.DTOs.Problems;
using ServerAPIApp.Core.Helpers;
using ServerAPIApp.Core.UseCases.ProblemVersions;
using ServerAPIApp.Domain.Entities;
using ServerAPIApp.Domain.Exceptions.InternalServerExceptions;
using System.Text.Json;

namespace ServerAPIApp.Core.UseCaseHandlers.ProblemVersions
{
    public class CreateDraftCaseHandler : IRequestHandler<CreateVersionDraftCase, EditorProblemVersionDto>
    {
        private static readonly JsonSerializerOptions _opts = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        };

        private IProblemVersionRepository _repo;
        private IProblemVersionLanguageRepository _versionLanguageRepository;
        private ILanguageRepository _languageRepository;
        private IObjectStorage _storage;

        //TODO: move this to config as well
        const string _bucketName = "manifestbucket";

        public CreateDraftCaseHandler(IProblemVersionRepository repo, IProblemVersionLanguageRepository versionLanguageRepo, ILanguageRepository languageRepo, IObjectStorage storage)
        {
            _repo = repo;
            _storage = storage;
            _languageRepository = languageRepo;
            _versionLanguageRepository = versionLanguageRepo;
        }

        public async Task<EditorProblemVersionDto> Handle(CreateVersionDraftCase command, CancellationToken cancellationToken)
        {
            var (draft, manifest) = command.ToEntity();

            var languages = await _languageRepository.GetAllAsync(cancellationToken);

            if (languages is null || languages.Count == 0)
                throw new EntityUpdateException("Couldn't find any language");

            if (manifest is not null)
            {
                var key = $"problems/{draft.ProblemId}/versions/{draft.Id}/template.json";

                var manifestJson = JsonSerializer.Serialize(manifest, _opts);

                if (!await _storage.UploadStringAsync(_bucketName, key, manifestJson, cancellationToken: cancellationToken))
                    throw new ObjectStorageUploadException("Failed to save tests.");

                draft.TestTemplateKey = key;
            }

            await _repo.CreateAsync(draft, cancellationToken);

            foreach (var language in languages)
            {
                var newEntity = new ProblemVersionLanguage()
                {
                    LanguageId = language.Id,
                    VersionId = draft.Id
                };

                await _versionLanguageRepository.CreateAsync(newEntity, cancellationToken);

                draft.SupportedLanguages.Add(newEntity);
            }

            return EditorProblemVersionDto.From(draft, manifest);
        }
    }
}
