using ServerAPIApp.Domain.Entities;

namespace ServerAPIApp.Core.Abstractions
{
    public interface IJwtTokenService
    {
        Task<string> UpdateAccessTokenAsync(string access, string deviceId, CancellationToken cancellationToken = default);
        Task LogoutUserAsync(Guid id, string devicedId, CancellationToken cancellationToken = default);
        string CreateNewRefreshToken();
        DateTimeOffset GetTokenExpirationTime(DateTimeOffset dateIssued);
        string GenerateAccessToken(List<string> roles, Guid userId);
    }
}
