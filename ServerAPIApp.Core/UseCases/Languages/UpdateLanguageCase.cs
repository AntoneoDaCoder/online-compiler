using MediatR;
using ServerAPIApp.Domain.Entities;

namespace ServerAPIApp.Core.UseCases.Languages
{
    public record UpdateLanguageCase(Guid Id, string DisplayName, string Code) : IRequest<LanguageEntity>;
}
