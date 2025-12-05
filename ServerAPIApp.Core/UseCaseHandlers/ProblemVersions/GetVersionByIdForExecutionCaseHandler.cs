using MediatR;
using ServerAPIApp.Contracts.Abstractions;
using ServerAPIApp.Contracts.DTOs;
using ServerAPIApp.Core.UseCases.ProblemVersions;
using ServerAPIApp.Domain.Exceptions;
using Shared.DTOs;
using System.Text.Json;

namespace ServerAPIApp.Core.UseCaseHandlers.ProblemVersions
{
    public class GetVersionByIdForExecutionCaseHandler : IRequestHandler<GetVersionByIdForExecutionCase, ExecutionDto>
    {
        private IProblemVersionRepository _repo;
        private IObjectStorage _storage;

        //TODO: move this to config as well
        const string _bucketName = "xdd";

        public GetVersionByIdForExecutionCaseHandler(IProblemVersionRepository repo, IObjectStorage storage)
        {
            _repo = repo;
            _storage = storage;
        }

        public async Task<ExecutionDto> Handle(GetVersionByIdForExecutionCase command, CancellationToken cancellationToken)
        {
            var entity = await _repo.GetByIdWithLanguagesAsync(command.VersionId, cancellationToken);

            if (entity is null)
                throw new ResourceNotFoundException("Version not found");

            if (entity.SupportedLanguages.Count == 0 || !entity.SupportedLanguages.Any(pl => pl.Language?.Code == command.LanguageCode))
                throw new UnsupportedLanguageException($"This problem does not support {command.LanguageCode} language");

            if (string.IsNullOrEmpty(entity.TestTemplateKey))
                throw new InvalidTestTemplateException("Empty test template key");

            var key = $"problems/{entity.ProblemId}/versions/{entity.Id}/template.json";

            var manifestString = await _storage.GetStringAsync(_bucketName, key, cancellationToken);

            if (manifestString is null)
                throw new InvalidTestTemplateException("Empty manifest data");

            var manifestDto = JsonSerializer.Deserialize<ManifestDto>(manifestString);

            if (manifestDto is null)
                throw new InvalidTestTemplateException("Failed to deserialize test template");

            return ExecutionDto.From(entity, manifestDto);
        }
    }
}
