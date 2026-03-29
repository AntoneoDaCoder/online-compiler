using ServerAPIApp.Contracts.DTOs;

namespace ServerAPIApp.Core.Abstractions
{
    public interface IKeycloakService
    {
        Task<KeycloakTokenResponseDto> LoginAsync
            (string email,
            string password,
            CancellationToken cancellationToken = default);

        Task<string> RegisterUserAsync(string email, string password, IEnumerable<string> roles, CancellationToken cancellationToken = default);

        Task<KeycloakUserResponseDto?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default);

        Task<KeycloakTokenResponseDto> RefreshAccessTokenAsync
            (string refreshToken,
            CancellationToken cancellationToken = default);

        Task LogoutAsync(string refreshToken, CancellationToken cancellationToken = default);

        Task DeleteAccountAsync(string keycloakAccountId, CancellationToken cancellationToken = default);

        Task<string?> GetUserEmailByIdAsync(string userId, CancellationToken cancellationToken = default);
    }
}
