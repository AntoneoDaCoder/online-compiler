using MediatR;
using ServerAPIApp.Domain.Entities;

namespace ServerAPIApp.Core.UseCases.Languages
{
    public record GetLanguageByCodeCase(string Code) : IRequest<LanguageEntity>;
}
