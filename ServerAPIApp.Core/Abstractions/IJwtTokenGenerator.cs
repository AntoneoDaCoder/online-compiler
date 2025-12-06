using System.Security.Claims;

namespace ServerAPIApp.Core.Abstractions
{
    public interface IJwtTokenGenerator
    {
        string GenerateAccessToken(IEnumerable<string> roles, Guid userId);
        string GenerateRefreshToken();
        Task<ClaimsPrincipal> GetPrincipalFromExpiredTokenAsync(string token, CancellationToken cancellationToken = default);
    }
}
