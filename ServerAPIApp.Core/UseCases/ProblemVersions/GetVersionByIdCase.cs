using MediatR;

namespace ServerAPIApp.Core.UseCases.ProblemVersions
{
    public record GetVersionByIdCase(Guid VersionId) : IRequest;
}
