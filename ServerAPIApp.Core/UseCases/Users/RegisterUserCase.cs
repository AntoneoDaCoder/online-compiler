using MediatR;

namespace ServerAPIApp.Core.UseCases.Users
{
    public record RegisterUserCase(string Name, string Email, string Password) : IRequest<string>;
}
