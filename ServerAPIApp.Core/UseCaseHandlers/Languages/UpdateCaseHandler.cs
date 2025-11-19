using MediatR;
using ServerAPIApp.Contracts.Abstractions;
using ServerAPIApp.Core.Helpers;
using ServerAPIApp.Core.UseCases.Languages;
using ServerAPIApp.Domain.Entities;
using ServerAPIApp.Domain.Exceptions;

namespace ServerAPIApp.Core.UseCaseHandlers.Languages
{
    public class UpdateCaseHandler : IRequestHandler<UpdateLanguageCase, LanguageEntity>
    {
        private ILanguageRepository _repo;
        public UpdateCaseHandler(ILanguageRepository repo)
        {
            _repo = repo;
        }

        //either way we must return the entity regardless of whether it has existed before or not (create otherwise)
        public async Task<LanguageEntity> Handle(UpdateLanguageCase command, CancellationToken cancellationToken)
        {
            var updated = command.ToEntity();

            var entity = await _repo.UpdateAsync(updated, cancellationToken);

            return entity;
        }
    }
}
