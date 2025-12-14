using MediatR;
using ServerAPIApp.Contracts.Abstractions;
using ServerAPIApp.Contracts.DTOs;
using ServerAPIApp.Core.UseCases.ProblemVersions;
using ServerAPIApp.Domain.Entities;
using Shared.DTOs;
using Shared.Helpers;
using System.Linq.Expressions;

namespace ServerAPIApp.Core.UseCaseHandlers.ProblemVersions
{
    public class GetFilteredVersionsCaseHandler : IRequestHandler<GetFilteredVersionsCase, IEnumerable<EditorProblemVersionDto>?>
    {
        private IProblemVersionRepository _repo;
        private IObjectStorage _storage;

        //TODO: move this to config as well
        const string _bucketName = "manifestbucket";

        public GetFilteredVersionsCaseHandler(IProblemVersionRepository repo, IObjectStorage storage)
        {
            _repo = repo;
            _storage = storage;
        }

        public async Task<IEnumerable<EditorProblemVersionDto>?> Handle(GetFilteredVersionsCase request, CancellationToken cancellationToken)
        {
            Expression<Func<ProblemVersionEntity, bool>> filter = x => true;

            var versions = await _repo.GetFilteredWithLanguagesAsync(filter, cancellationToken);

            var result = new List<EditorProblemVersionDto>();

            if (versions != null)
                foreach (var version in versions)
                {
                    var key = $"problems/{version.ProblemId}/versions/{version.Id}/template.json";

                    var manifestString = await _storage.GetStringAsync(_bucketName, key, cancellationToken);

                    ManifestDto? manifest = null;

                    if (manifestString != null)
                        manifest = ManifestParser.Parse(manifestString);

                    var newDto = EditorProblemVersionDto.From(version, manifest);

                    result.Add(newDto);
                }

            return result;
        }
    }
}
