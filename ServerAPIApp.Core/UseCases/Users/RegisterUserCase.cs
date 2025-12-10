using MediatR;
using ServerAPIApp.Contracts.DTOs;

namespace ServerAPIApp.Core.UseCases.Users
{
    public record RegisterUserCase(string Name, string Email, string Password) : IRequest<LoginDataDto>;
}
