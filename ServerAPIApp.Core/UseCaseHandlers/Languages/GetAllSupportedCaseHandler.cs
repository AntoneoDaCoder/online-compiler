using MediatR;
using ServerAPIApp.Contracts.Abstractions;
using ServerAPIApp.Contracts.DTOs;
using ServerAPIApp.Core.UseCases.Languages;
using ServerAPIApp.Core.Helpers;

namespace ServerAPIApp.Core.UseCaseHandlers.Languages
{
    public class GetAllSupportedCaseHandler : IRequestHandler<GetAllSupportedLanguagesCase, IEnumerable<LanguageDto>?>
    {
        private ILanguageRepository _repo;

        public GetAllSupportedCaseHandler(ILanguageRepository repo)
        {
            _repo = repo;
        }

        public async Task<IEnumerable<LanguageDto>?> Handle(GetAllSupportedLanguagesCase request, CancellationToken cancellationToken)
        {
            var entities = await _repo.GetAllAsync(cancellationToken);

            return entities.ToDto();
        }
    }
}
