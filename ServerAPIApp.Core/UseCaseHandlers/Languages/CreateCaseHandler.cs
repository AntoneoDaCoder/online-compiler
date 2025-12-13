using MediatR;
using ServerAPIApp.Contracts.Abstractions;
using ServerAPIApp.Contracts.DTOs;
using ServerAPIApp.Core.Helpers;
using ServerAPIApp.Core.UseCases.Languages;
using ServerAPIApp.Domain.Entities;
using ServerAPIApp.Domain.Exceptions.BadRequestExceptions;


//TODO: add validators
namespace ServerAPIApp.Core.UseCaseHandlers.Languages
{
    public class CreateCaseHandler : IRequestHandler<CreateLanguageCase, LanguageDto>
    {
        private readonly ILanguageRepository _repo;

        public CreateCaseHandler(ILanguageRepository repo)
        {
            _repo = repo;
        }

        public async Task<LanguageDto> Handle(CreateLanguageCase command, CancellationToken cancellationToken)
        {
            var entity = command.ToEntity();

            if (string.IsNullOrEmpty(entity.Code))
                throw new EmptyFieldException("Code cannot be empty");

            if (string.IsNullOrEmpty(entity.DisplayName))
                throw new EmptyFieldException("DisplayName cannot be empty");

            return LanguageDto.From(await _repo.CreateAsync(entity, cancellationToken));
        }
    }
}
