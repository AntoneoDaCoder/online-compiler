using MediatR;

namespace ServerAPIApp.Core.UseCases.Languages
{
    public record CreateLanguageCase(string DisplayName, string Code) : IRequest;
}
