using MediatR;
using ServerAPIApp.Contracts.Abstractions;
using ServerAPIApp.Core.Helpers;
using ServerAPIApp.Core.UseCases.Languages;
using ServerAPIApp.Domain.Entities;


//TODO: add validators
namespace ServerAPIApp.Core.UseCaseHandlers.Languages
{
    public class CreateCaseHandler : IRequestHandler<CreateLanguageCase, LanguageEntity>
    {
        private readonly ILanguageRepository _repo;

        public CreateCaseHandler(ILanguageRepository repo)
        {
            _repo = repo;
        }

        public async Task<LanguageEntity> Handle(CreateLanguageCase command, CancellationToken cancellationToken)
        {
            var entity = command.ToEntity();

            return await _repo.CreateAsync(entity, cancellationToken);
        }
    }
}
