using MediatR;
using ServerAPIApp.Contracts.Abstractions;
using ServerAPIApp.Core.UseCases.Languages;
using ServerAPIApp.Domain.Entities;
using ServerAPIApp.Domain.Exceptions;

namespace ServerAPIApp.Core.UseCaseHandlers.Languages
{
    public class GetByCodeCaseHandler : IRequestHandler<GetLanguageByCodeCase, LanguageEntity>
    {
        private ILanguageRepository _repo;

        public GetByCodeCaseHandler(ILanguageRepository repo)
        {
            _repo = repo;
        }

        public async Task<LanguageEntity> Handle(GetLanguageByCodeCase command, CancellationToken cancellationToken)
        {
            var entity = await _repo.GetByCodeAsync(command.Code, cancellationToken);

            if (entity is null)
                throw new ResourceNotFoundException("Resource not found");

            return entity;
        }
    }
}
