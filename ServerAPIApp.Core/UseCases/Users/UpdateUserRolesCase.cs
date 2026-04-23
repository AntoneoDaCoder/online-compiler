using MediatR;
using ServerAPIApp.Contracts.DTOs.Auth;

namespace ServerAPIApp.Core.UseCases.Users
{
    public record UpdateUserRolesCase(string UserId, IEnumerable<string> RolesToAdd, IEnumerable<string> RolesToRemove) : IRequest
    {
        public static UpdateUserRolesCase From(string userId, UpdateRolesDto dto)
        {
            var assign = dto.RolesToAdd ?? [];
            var remove = dto.RolesToRemove ?? [];

            return new UpdateUserRolesCase(userId, assign, remove);
        }
    }
}
