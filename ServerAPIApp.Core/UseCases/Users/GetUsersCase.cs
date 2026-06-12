using MediatR;
using ServerAPIApp.Contracts.DTOs.Auth;

namespace ServerAPIApp.Core.UseCases.Users
{
    public record GetUsersCase() : IRequest<IEnumerable<ExternalUserResponseDto>?>;
}
