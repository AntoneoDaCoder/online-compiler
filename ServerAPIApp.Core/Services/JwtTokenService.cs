using Microsoft.Extensions.Logging;
using ServerAPIApp.Contracts.Abstractions;
using ServerAPIApp.Core.Abstractions;
using ServerAPIApp.Domain.Entities;
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

        public async Task<string> UpdateAccessTokenAsync(string oldAccessToken, string deviceId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("{ServiceName} is starting access token update sequence", GetType().Name);

            var principal = await _tokenGenerator.GetPrincipalFromExpiredTokenAsync(oldAccessToken);

            var userId = Guid.Parse(principal.Claims.First(x => x.Type == ClaimTypes.UserData).Value);

            _logger.LogInformation("{ServiceName} is trying to get user's [Id:{UserId}] data", GetType().Name, userId);

            var (userEntity, roles) = await _userRepository
                 .GetByIdWithRolesAsync(userId, cancellationToken);

            if (userEntity is null)
                throw new ResourceNotFoundException("User not found");

            if (roles is null || roles.Count == 0)
                throw new EmptyRolesException("User must have at least 1 role");

            _logger.LogInformation("{ServiceName} obtained user's [Id:{UserId}] data", GetType().Name, userId);

            _logger.LogInformation("{ServiceName} is trying to get user's [Id:{UserId}] valid refresh token", GetType().Name, userId);

            if (userEntity.RefreshToken is null)
                throw new InvalidTokenException("Invalid refresh token");

            _logger.LogInformation("{ServiceName} obtained user's [Id:{UserId}] valid refresh token, generating new access token", GetType().Name, userId);

            return _tokenGenerator.GenerateAccessToken(roles, userId);
        }

        public async Task LogoutUserAsync(Guid id, string deviceId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("{ServiceName} is starting user logout sequence", GetType().Name);

            if (string.IsNullOrEmpty(deviceId))
                throw new EmptyDeviceIdException("DeviceId must be set to logout");

            _logger.LogInformation("{ServiceName} is trying to get user's [Id:{UserId}] valid refresh token", GetType().Name, id);

            var user = await _userRepository.GetByIdAsync(id, cancellationToken);

            if (user is null)
                throw new ResourceNotFoundException("User not found");

            if (user.RefreshToken is null)
                throw new InvalidTokenException("Invalid refresh token");

            user.RefreshToken = null;

            _logger.LogInformation("{ServiceName} obtained user's [Id:{UserId}] valid refresh token, revoking this token", GetType().Name, id);

            await _userRepository.UpdateAsync(user, cancellationToken);
        }

        public string CreateNewRefreshToken()
        {
            return _tokenGenerator.GenerateRefreshToken();
        }

        public DateTimeOffset GetTokenExpirationTime(DateTimeOffset dateIssued)
        {
            return dateIssued.Add(_tokenLifetime);
        }

        public string GenerateAccessToken(List<string> roles, Guid userId)
        {
            return _tokenGenerator.GenerateAccessToken(roles, userId);
        }
    }
}

