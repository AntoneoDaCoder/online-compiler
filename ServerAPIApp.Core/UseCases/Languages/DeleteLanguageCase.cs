using MediatR;

namespace ServerAPIApp.Core.UseCases.Languages
{
    public record DeleteLanguageCase(Guid Id) : IRequest;
}
