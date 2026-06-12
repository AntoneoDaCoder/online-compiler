using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ServerAPIApp.Contracts.Abstractions;
using ServerAPIApp.Core.Abstractions;
using ServerAPIApp.Core.UseCases.ProblemVersions;
using ServerAPIApp.DAL.Confs;
using ServerAPIApp.Domain.Exceptions.BadRequestExceptions;
using ServerAPIApp.Domain.Exceptions.InternalServerExceptions;
using ServerAPIApp.Domain.Exceptions.NotFoundExceptions;
using Shared.Helpers;

namespace ServerAPIApp.Core.UseCaseHandlers.ProblemVersions
{
    public class GetEntrypointCodeTemplateCaseHandler : IRequestHandler<GetEntrypointCodeTemplateCase, string>
    {
        private readonly IProblemVersionRepository _repo;
        private readonly IServiceProvider _sp;
        private readonly IObjectStorage _storage;
        private readonly string _bucketName;

        public GetEntrypointCodeTemplateCaseHandler(IProblemVersionRepository repo, IObjectStorage storage, IServiceProvider sp, IOptions<MinioConfiguration> conf)
        {
            _repo = repo;
            _storage = storage;
            _bucketName = conf.Value.BucketName;
            _sp = sp;
        }

        public async Task<string> Handle(GetEntrypointCodeTemplateCase request, CancellationToken cancellationToken)
        {
            var version = await _repo
                .Query()
                .AsNoTracking()
                .Where(x => x.Id == request.VersionId)
                .Include(x => x.SupportedLanguages)
                .ThenInclude(x => x.Language)
                .FirstOrDefaultAsync(cancellationToken);

            if (version is null)
                throw new ResourceNotFoundException("Version not found");

            if (string.IsNullOrEmpty(version.TestTemplateKey))
                throw new InvalidTestTemplateException("Empty test template key");

            var langCode = version.SupportedLanguages.Where(x => x.LanguageId == request.LanguageId).Select(x => x.Language!.Code).FirstOrDefault();

            if (langCode is null)
                throw new BadRequestException("Version does not support such programming language.");

            var key = $"problems/{version.ProblemId}/versions/{version.Id}/template.json";

            var manifestString = await _storage.GetStringAsync(_bucketName, key, cancellationToken);

            if (manifestString is null)
                throw new InvalidTestTemplateException("Empty manifest data");

            var manifest = ManifestParser.Parse(manifestString);

            var generator = _sp.GetKeyedService<ITemplateGenerator>(langCode);

            if (generator is null)
                throw new InternalServerException($"No template generator was configured for {langCode} language");

            var template = generator.BuildTemplate(manifest);

            return template;
        }
    }
}
