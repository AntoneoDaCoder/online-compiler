using MediatR;

//TODO: after im done with all use cases, update their return types accordingly

namespace ServerAPIApp.Core.UseCases.Languages
{
    public record CreateLanguageCase(string DisplayName, string Code) : IRequest;
}
