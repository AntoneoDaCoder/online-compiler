using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ServerAPIApp.Contracts.Abstractions;
using ServerAPIApp.Contracts.DTOs.Problems;
using ServerAPIApp.Core.UseCases.Problems;
using ServerAPIApp.DAL.Confs;
using ServerAPIApp.Domain.Exceptions.NotFoundExceptions;
using Shared.DTOs;
using Shared.Helpers;

namespace ServerAPIApp.Core.UseCaseHandlers.Problems
{
    public class GetProblemLatestVersionCaseHandler : IRequestHandler<GetProblemLatestVersionCase, EditorProblemVersionDto>
    {
        private IProblemRepository _repo;
        private IObjectStorage _storage;
        private readonly string _bucketName;

        public GetProblemLatestVersionCaseHandler(IProblemRepository repo, IObjectStorage storage, IOptions<MinioConfiguration> opt)
        {
            _repo = repo;
            _storage = storage;
            _bucketName = opt.Value.BucketName;
        }

        public async Task<EditorProblemVersionDto> Handle(GetProblemLatestVersionCase command, CancellationToken cancellationToken)
        {
            var problem = await _repo
                .Query()
                .AsNoTracking()
                .Where(x => x.Slug == command.ProblemSlug)
                .Include(x => x.LastPublishedVersion)
                .ThenInclude(x => x.SupportedLanguages)
                .FirstOrDefaultAsync(cancellationToken);

            if (problem is null)
                throw new ResourceNotFoundException("Such problem doesn't exist");

            if (problem.LastPublishedVersion is null)
                throw new ResourceNotFoundException("Problem doesn't have a published version");

            Console.WriteLine("[API] Found problem");

            var key = $"problems/{problem.Id}/versions/{problem.LastPublishedVersion.Id}/template.json";

            var manifestString = await _storage.GetStringAsync(_bucketName, key, cancellationToken);

            ManifestDto? manifest = null;

            if (manifestString != null)
                manifest = ManifestParser.Parse(manifestString);

            return EditorProblemVersionDto.From(problem.LastPublishedVersion, manifest);
        }
    }
}
