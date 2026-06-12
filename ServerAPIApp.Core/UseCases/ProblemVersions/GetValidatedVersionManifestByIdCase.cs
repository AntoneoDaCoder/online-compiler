using MediatR;
using ServerAPIApp.Contracts.DTOs;

namespace ServerAPIApp.Core.UseCases.ProblemVersions
{
    public record GetValidatedVersionManifestByIdCase(Guid VersionId, string LanguageCode) : IRequest<string>;
}
