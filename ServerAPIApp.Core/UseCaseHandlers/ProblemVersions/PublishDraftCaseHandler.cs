using MediatR;
using ServerAPIApp.Contracts.Abstractions;
using ServerAPIApp.Core.UseCases.ProblemVersions;
using ServerAPIApp.Domain.Exceptions.NotFoundExceptions;
using ServerAPIApp.Domain.Exceptions.InternalServerExceptions;

namespace ServerAPIApp.Core.UseCaseHandlers.ProblemVersions
{
    public class PublishDraftCaseHandler : IRequestHandler<PublishVersionDraftCase>
    {
        private IProblemVersionRepository _repo;

        public PublishDraftCaseHandler(IProblemVersionRepository repo)
        {
            _repo = repo;
        }

        public async Task Handle(PublishVersionDraftCase command, CancellationToken cancellationToken)
        {
            var entity = await _repo.GetByIdAsync(command.DraftId, isDraft: true, cancellationToken);

            if (entity is null)
                throw new ResourceNotFoundException("Resource not found");

            if (string.IsNullOrEmpty(entity.TestTemplateKey))
                throw new DraftPublishException($"Can't publish {command.DraftId} without tests");

            // НЕ менять entity.IsPublished / entity.PublishedBy здесь.
            var res = await _repo.PublishDraftAsync(command.DraftId, command.PublisherId, cancellationToken);

            if (res is null)
                throw new DraftPublishException($"Failed to publish {command.DraftId}");
        }

    }
}
