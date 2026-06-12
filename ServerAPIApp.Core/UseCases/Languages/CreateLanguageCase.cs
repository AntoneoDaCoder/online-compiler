using MediatR;
using ServerAPIApp.Contracts.DTOs;

namespace ServerAPIApp.Core.UseCases.Languages
{
    public record CreateLanguageCase(string DisplayName, string Code) : IRequest<LanguageDto>;
}
