using MediatR;
using ServerAPIApp.Domain.Entities;

namespace ServerAPIApp.Core.UseCases.Languages
{
    public record GetLanguageByIdCase(Guid Id) : IRequest<LanguageEntity>;
}
