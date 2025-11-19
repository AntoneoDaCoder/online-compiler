using MediatR;
using ServerAPIApp.Contracts.Abstractions;
using ServerAPIApp.Core.UseCases.Languages;
using ServerAPIApp.Domain.Entities;
using ServerAPIApp.Domain.Exceptions;

namespace ServerAPIApp.Core.UseCaseHandlers.Languages
{
    public class GetByIdHandler : IRequestHandler<GetLanguageByIdCase, LanguageEntity>
    {
        private ILanguageRepository _repo;

        public GetByIdHandler(ILanguageRepository repo)
        {
            _repo = repo;
        }

        public async Task<LanguageEntity> Handle(GetLanguageByIdCase command, CancellationToken cancellationToken)
        {
            var entity = await _repo.GetByIdAsync(command.Id, cancellationToken);

            if (entity is null)
                throw new ResourceNotFoundException("Resource not found");

            return entity;
        }
    }
}
