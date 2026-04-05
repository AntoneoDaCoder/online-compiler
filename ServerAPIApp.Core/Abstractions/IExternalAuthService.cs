using ServerAPIApp.Contracts.DTOs.Auth;

namespace ServerAPIApp.Core.Abstractions
{
    public interface IExternalAuthService
    {
        Task<ExternalUserResponseDto?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default);

        Task DeleteAccountAsync(string keycloakAccountId, CancellationToken cancellationToken = default);

        Task<string?> GetUserEmailByIdAsync(string userId, CancellationToken cancellationToken = default);

        Task AssignRealmRolesAsync(string userId, IEnumerable<string> roleNames, CancellationToken cancellationToken);
    }
}
