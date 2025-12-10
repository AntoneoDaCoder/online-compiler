using MediatR;
using ServerAPIApp.Contracts.Abstractions;
using ServerAPIApp.Contracts.DTOs;
using ServerAPIApp.Core.Helpers;
using ServerAPIApp.Core.UseCases.Languages;
using ServerAPIApp.Domain.Entities;
using ServerAPIApp.Domain.Exceptions.BadRequestExceptions;
using ServerAPIApp.Domain.Exceptions.NotFoundExceptions;

namespace ServerAPIApp.Core.UseCaseHandlers.Languages
{
    public class UpdateCaseHandler : IRequestHandler<UpdateLanguageCase, LanguageDto>
    {
        private ILanguageRepository _repo;
        public UpdateCaseHandler(ILanguageRepository repo)
        {
            _repo = repo;
        }

        public async Task<LanguageDto> Handle(UpdateLanguageCase command, CancellationToken cancellationToken)
        {
            var entity = command.ToEntity();

            if (string.IsNullOrEmpty(entity.Code))
                throw new EmptyFieldException("Code cannot be empty");

            if (string.IsNullOrEmpty(entity.DisplayName))
                throw new EmptyFieldException("DisplayName cannot be empty");

            var updated = await _repo.UpdateAsync(entity, cancellationToken);

            if (!updated)
                throw new ResourceNotFoundException("Resource not found");

            return LanguageDto.From(entity);
        }
    }
}
