using MediatR;

namespace ServerAPIApp.Core.UseCases.Users
{
    public record InternalLoginUserCase(string Email, string Password) : IRequest<string>;
}
