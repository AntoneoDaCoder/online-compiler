using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ServerAPIApp.Contracts.Abstractions;
using ServerAPIApp.Contracts.DTOs.Problems;
using ServerAPIApp.Core.UseCases.ProblemVersions;
using ServerAPIApp.DAL.Confs;
using ServerAPIApp.Domain.Exceptions.NotFoundExceptions;
using Shared.DTOs;
using Shared.Helpers;

namespace ServerAPIApp.Core.UseCaseHandlers.ProblemVersions
{
    public class GetEditorVersionCaseHandler : IRequestHandler<GetEditorVersionCase, EditorProblemVersionDto?>
    {
        private IProblemVersionRepository _repo;
        private IObjectStorage _storage;

        private readonly string _bucketName;

        public GetEditorVersionCaseHandler(IProblemVersionRepository repo, IObjectStorage storage, IOptions<MinioConfiguration> conf)
        {
            _repo = repo;
            _storage = storage;
            _bucketName = conf.Value.BucketName;
        }

        public async Task<EditorProblemVersionDto?> Handle(GetEditorVersionCase command, CancellationToken cancellationToken)
        {
            var version = await _repo.Query()
                .AsNoTracking()
                .Where(x => x.Id == command.VersionId)
                .Include(x => x.SupportedLanguages)
                .ThenInclude(x => x.Language)
                .FirstOrDefaultAsync(cancellationToken);

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
