using MediatR;
using ServerAPIApp.Contracts.DTOs.Auth;

namespace ServerAPIApp.Core.UseCases.Users
{
    public record GetAvailableRolesCase() : IRequest<IEnumerable<ExternalRoleDto>?>;
}
