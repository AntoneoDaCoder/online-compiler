using MediatR;

namespace ServerAPIApp.Core.UseCases.ProblemVersions
{
    public record PublishVersionDraftCase(Guid DraftId, Guid PublisherId) : IRequest;
}
