using MediatR;

namespace ServerAPIApp.Core.UseCases.ProblemVersions
{
    public record DeleteVersionDraftCase(Guid DraftId) : IRequest;
}
