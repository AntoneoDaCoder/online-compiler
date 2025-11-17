using MediatR;

namespace ServerAPIApp.Core.UseCases.Languages
{
    public record UpdateLanguageCase(Guid Id, string DisplayName, string Code) : IRequest;
}
