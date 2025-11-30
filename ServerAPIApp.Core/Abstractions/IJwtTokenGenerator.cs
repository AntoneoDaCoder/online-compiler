using ServerAPIApp.Domain.Entities;
using System.Security.Claims;

namespace ServerAPIApp.Core.Abstractions
{
    public interface IJwtTokenGenerator
    {
        string GenerateAccessToken(List<string> roles, Guid userId);
        string GenerateRefreshToken();
        Task<ClaimsPrincipal> GetPrincipalFromExpiredTokenAsync(string token);
    }
}
