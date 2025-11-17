using MediatR;

namespace ServerAPIApp.Core.UseCases.Languages
{
    public record GetLanguageByIdCase(Guid Id) : IRequest;
}
