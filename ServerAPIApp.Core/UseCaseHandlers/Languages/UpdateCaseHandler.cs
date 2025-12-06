using MediatR;
using ServerAPIApp.Contracts.Abstractions;
using ServerAPIApp.Core.Helpers;
using ServerAPIApp.Core.UseCases.Languages;
using ServerAPIApp.Domain.Entities;
using ServerAPIApp.Domain.Exceptions.NotFoundExceptions;

namespace ServerAPIApp.Core.UseCaseHandlers.Languages
{
    public class UpdateCaseHandler : IRequestHandler<UpdateLanguageCase, LanguageEntity>
    {
        private ILanguageRepository _repo;
        public UpdateCaseHandler(ILanguageRepository repo)
        {
            _repo = repo;
        }

        public async Task<LanguageEntity> Handle(UpdateLanguageCase command, CancellationToken cancellationToken)
        {
            var entity = command.ToEntity();

            var updated = await _repo.UpdateAsync(entity, cancellationToken);

            if (!updated)
                throw new ResourceNotFoundException("Resource not found");

            return entity;
        }
    }
}
