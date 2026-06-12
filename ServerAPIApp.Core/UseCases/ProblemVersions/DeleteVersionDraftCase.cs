using MediatR;

namespace ServerAPIApp.Core.UseCases.ProblemVersions
{
    public record DeleteVersionDraftCase(Guid Id, Guid ProblemId) : IRequest;
}
