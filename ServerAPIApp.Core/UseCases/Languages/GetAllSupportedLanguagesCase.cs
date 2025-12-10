using MediatR;
using ServerAPIApp.Contracts.DTOs;

namespace ServerAPIApp.Core.UseCases.Languages
{
    public record GetAllSupportedLanguagesCase() : IRequest<IEnumerable<LanguageDto>?>;
}
