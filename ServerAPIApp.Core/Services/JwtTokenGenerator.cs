using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using ServerAPIApp.Core.Abstractions;
using System.Security.Claims;
using System.Security.Cryptography;
using ServerAPIApp.Core.Configs;
using System.Text;

namespace ServerAPIApp.Core.Services
{
    public class JwtTokenGenerator : IJwtTokenGenerator
    {
        private readonly JwtSettings _jwtSettings;

        public JwtTokenGenerator(IOptionsMonitor<JwtSettings> jwtSettings)
        {
            _jwtSettings = jwtSettings.CurrentValue;
        }

        public string GenerateAccessToken(IEnumerable<string> roles, Guid userId)
        {
            var signingCredentials = GetSigningCredentials();

            var claims = GetClaims(roles, userId.ToString());

            return GenerateAccessToken(signingCredentials, claims);
        }

        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];

            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);

                return Convert.ToBase64String(randomNumber);
            }
        }

        public async Task<ClaimsPrincipal> GetPrincipalFromExpiredTokenAsync(string token, CancellationToken cancellationToken = default)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = true,
                ValidateIssuer = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(
                   Encoding.UTF8.GetBytes(_jwtSettings.SecretKey)),
                ValidateLifetime = false,
                ValidIssuer = _jwtSettings.ValidIssuer,
                ValidAudience = _jwtSettings.ValidAudience,
                RoleClaimType = ClaimTypes.Role,

            };

            var tokenHandler = new JsonWebTokenHandler();

            var tokenValidationResult = await tokenHandler.ValidateTokenAsync(token, tokenValidationParameters);

            if (tokenValidationResult.IsValid)
            {
                return new ClaimsPrincipal(tokenValidationResult.ClaimsIdentity);
            }

            throw new SecurityTokenException("Invalid token");
        }

        private SigningCredentials GetSigningCredentials()
        {
            var key = Encoding.UTF8.GetBytes(_jwtSettings.SecretKey);

            var secret = new SymmetricSecurityKey(key);

            return new SigningCredentials(secret, SecurityAlgorithms.HmacSha256);
        }

        private static List<Claim> GetClaims(IEnumerable<string> roles, string userId)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, userId)
            };

            foreach (var role in roles)
                claims.Add(new(ClaimTypes.Role, role));

            return claims;
        }

        private string GenerateAccessToken(SigningCredentials signingCredentials, List<Claim> claims)
        {
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Issuer = _jwtSettings.ValidIssuer,
                Audience = _jwtSettings.ValidAudience,
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryInMinutes),
                SigningCredentials = signingCredentials
            };

            var tokenHandler = new JsonWebTokenHandler();

            return tokenHandler.CreateToken(tokenDescriptor);
        }
    }
}

