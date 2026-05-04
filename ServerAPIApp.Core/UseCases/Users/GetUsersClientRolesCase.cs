using MediatR;
using ServerAPIApp.Contracts.DTOs.Auth;

namespace ServerAPIApp.Core.UseCases.Users
{
    public record GetUsersClientRolesCase(string UserId) : IRequest<IEnumerable<ExternalRoleDto>?>;
}
