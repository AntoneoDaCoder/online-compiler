using MediatR;

namespace ServerAPIApp.Core.UseCases.ProblemVersions
{
    public record GetEntrypointCodeTemplateCase(Guid VersionId, Guid LanguageId) : IRequest<string>;
}
