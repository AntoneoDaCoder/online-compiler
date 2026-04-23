using ServerAPIApp.Contracts.DTOs.Auth;

namespace ServerAPIApp.Core.Abstractions
{
    public interface IExternalAuthService
    {
        Task<ExternalUserResponseDto?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default);

        Task<IEnumerable<ExternalUserResponseDto>?> GetAllUsersAsync(CancellationToken cancellationToken = default);

        Task<IEnumerable<ExternalRoleDto>?> GetUserRealmRolesAsync(
           string userId,
           CancellationToken cancellationToken = default);

        Task<Guid> CreateUserWithRolesAsync(string email, string username, string password, IEnumerable<string> roles,
            CancellationToken cancellationToken = default);

        Task DeleteAccountAsync(string keycloakAccountId, CancellationToken cancellationToken = default);

        Task<string?> GetUserEmailByIdAsync(string userId, CancellationToken cancellationToken = default);

        Task<IEnumerable<ExternalRoleDto>?> GetAvailableRolesAsync(CancellationToken cancellationToken = default);

        Task UpdateUserRolesAsync(string userId, IEnumerable<string> rolesToDelete, IEnumerable<string> rolesToAdd, CancellationToken cancellationToken = default);
    }
}
