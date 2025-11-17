using MediatR;

namespace ServerAPIApp.Core.UseCases.Languages
{
    public record GetLanguageByCodeCase(string Code) : IRequest;
}
