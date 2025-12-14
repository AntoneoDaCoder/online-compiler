using MediatR;
using ServerAPIApp.Contracts.Abstractions;
using ServerAPIApp.Contracts.DTOs;
using ServerAPIApp.Core.UseCases.Problems;
using ServerAPIApp.Domain.Exceptions.NotFoundExceptions;
using Shared.DTOs;
using Shared.Helpers;

namespace ServerAPIApp.Core.UseCaseHandlers.Problems
{
    public class GetProblemLatestVersionCaseHandler : IRequestHandler<GetProblemLatestVersionCase, EditorProblemVersionDto>
    {
        private IProblemRepository _repo;
        private IObjectStorage _storage;

        //TODO: move this to config as well
        const string _bucketName = "manifestbucket";

        public GetProblemLatestVersionCaseHandler(IProblemRepository repo, IObjectStorage storage)
        {
            _repo = repo;
            _storage = storage;
        }

        public async Task<EditorProblemVersionDto> Handle(GetProblemLatestVersionCase command, CancellationToken cancellationToken)
        {
            var problem = await _repo.GetLatestVersionBySlugAsync(command.ProblemSlug, cancellationToken);

            if (problem is null)
                throw new ResourceNotFoundException("Such problem doesn't exist");

            if (problem.LastPublishedVersion is null)
                throw new ResourceNotFoundException("Problem doesn't have a published version");

            var key = $"problems/{problem.Id}/versions/{problem.LastPublishedVersion.Id}/template.json";

            var manifestString = await _storage.GetStringAsync(_bucketName, key, cancellationToken);

            ManifestDto? manifest = null;

            if (manifestString != null)
                manifest = ManifestParser.Parse(manifestString);

            return EditorProblemVersionDto.From(problem.LastPublishedVersion, manifest);
        }
    }
}
