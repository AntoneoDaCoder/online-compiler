using ServerAPIApp.Domain.Entities;

namespace ServerAPIApp.Core.Abstractions
{
    public interface IJwtTokenService
    {
        Task<string> UpdateAccessTokenAsync(string access, string deviceId, CancellationToken cancellationToken = default);
        Task LogoutUserAsync(Guid id, string devicedId, CancellationToken cancellationToken = default);
        Task CreateNewRefreshTokenAsync(UserEntity userEntity, string deviceId, CancellationToken cancellationToken = default);
        string GenerateAccessToken(List<string> roles, Guid userId);
    }
}
