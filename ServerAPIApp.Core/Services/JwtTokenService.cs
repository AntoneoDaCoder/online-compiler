using Microsoft.Extensions.Options;
using ServerAPIApp.Core.Abstractions;
using ServerAPIApp.Core.Configs;
using ServerAPIApp.Domain.Exceptions.BadRequestExceptions;
using System.Globalization;
using System.Security.Claims;

namespace ServerAPIApp.Core.Services
{
    public class JwtTokenService : IJwtTokenService
    {
        private readonly JwtSettings _settings;
        private readonly TimeSpan _tokenLifetime;

        private readonly IJwtTokenGenerator _tokenGenerator;

        public JwtTokenService(IJwtTokenGenerator generator, IOptionsMonitor<JwtSettings> monitor)
        {
            _tokenGenerator = generator;

            _settings = monitor.CurrentValue;

            string refreshTokenLifetime = _settings.RefreshTokenLifetime;

            if (!TimeSpan.TryParse(refreshTokenLifetime, CultureInfo.InvariantCulture, out _tokenLifetime))
                throw new IncorrectTokenFormatException("Token service failed to parse token's lifetime. Incorrect data format (expected ISO 8601)");
        }

        public async Task<Guid> GetUserIdFromTokenAsync(string accessToken, CancellationToken cancellationToken = default)
        {
            var principal = await _tokenGenerator.GetPrincipalFromExpiredTokenAsync(accessToken, cancellationToken);

            var parsedId = Guid.Parse(principal.Claims.First(x => x.Type == ClaimTypes.NameIdentifier).Value);

            return parsedId;
        }

        public string CreateNewRefreshToken()
        {
            return _tokenGenerator.GenerateRefreshToken();
        }

        public DateTimeOffset GetTokenExpirationTime(DateTimeOffset dateIssued)
        {
            return dateIssued.Add(_tokenLifetime);
        }

        public string GenerateAccessToken(IEnumerable<string> roles, Guid userId)
        {
            return _tokenGenerator.GenerateAccessToken(roles, userId);
        }
    }
}

