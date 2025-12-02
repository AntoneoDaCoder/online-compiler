using Microsoft.Extensions.Logging;
using ServerAPIApp.Contracts.Abstractions;
using ServerAPIApp.Core.Abstractions;
using ServerAPIApp.Domain.Exceptions;
using System.Globalization;
using System.Security.Claims;

namespace ServerAPIApp.Core.Services
{
    public class JwtTokenService : IJwtTokenService
    {
        private const string RefreshTokenLifetimeKey = "REFRESH_TOKEN_LIFETIME";
        private readonly TimeSpan _tokenLifetime;

        private readonly IJwtTokenGenerator _tokenGenerator;
        private readonly IUserRepository _userRepository;
        private readonly ISecretProtector _protector;
        private readonly ILogger<JwtTokenService> _logger;

        public JwtTokenService(IUserRepository userRepository, IJwtTokenGenerator generator, ISecretProtector protector, ILogger<JwtTokenService> logger)
        {
            _tokenGenerator = generator;
            _userRepository = userRepository;
            _protector = protector;
            _logger = logger;

            string refreshTokenLifetime = Environment.GetEnvironmentVariable(RefreshTokenLifetimeKey)!;

            if (!TimeSpan.TryParse(refreshTokenLifetime, CultureInfo.InvariantCulture, out _tokenLifetime))
                throw new IncorrectTokenFormatException("Token service failed to parse token's lifetime. Incorrect data format (expected ISO 8601)");
        }

        public async Task<Guid> GetUserIdFromTokenAsync(string accessToken, CancellationToken cancellationToken = default)
        {
            var principal = await _tokenGenerator.GetPrincipalFromExpiredTokenAsync(accessToken, cancellationToken);

            var parsedId = Guid.Parse(principal.Claims.First(x => x.Type == ClaimTypes.UserData).Value);

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

