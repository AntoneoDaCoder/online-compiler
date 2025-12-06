using MediatR;
using ServerAPIApp.Contracts.Abstractions;
using ServerAPIApp.Core.Abstractions;
using ServerAPIApp.Core.UseCases.Users;
using ServerAPIApp.Domain.Exceptions.NotFoundExceptions;
using ServerAPIApp.Domain.Exceptions.ForbiddenExceptions;
using ServerAPIApp.Domain.Exceptions.UnauthorizedExceptions;

namespace ServerAPIApp.Core.UseCaseHandlers.Users
{
    public class RefreshAccessTokenCaseHandler : IRequestHandler<RefreshAccessTokenCase, string>
    {
        private IUserRepository _repo;
        private IJwtTokenService _tokenService;

        public RefreshAccessTokenCaseHandler(IUserRepository repo, IJwtTokenService service)
        {
            _repo = repo;
            _tokenService = service;
        }

        public async Task<string> Handle(RefreshAccessTokenCase command, CancellationToken cancellationToken)
        {
            var tokenUserId = await _tokenService.GetUserIdFromTokenAsync(command.AccessToken, cancellationToken);

            if (command.InitiatorId != tokenUserId)
                throw new RestrictedActionException("Failed to refresh access token, token mismatch detected");

            var (user, roles) = await _repo.GetByIdWithRolesAsync(tokenUserId, cancellationToken);

            if (user is null)
                throw new ResourceNotFoundException("Resource not found");

            if (user.RefreshToken is null || user.RefreshTokenExpiryTime is null || user.RefreshTokenExpiryTime < DateTimeOffset.UtcNow)
                throw new InvalidRefreshTokenException("Invalid refresh token, failed to issue access token");

            var now = DateTimeOffset.UtcNow;

            user.RefreshToken = _tokenService.CreateNewRefreshToken();
            user.RefreshTokenExpiryTime = _tokenService.GetTokenExpirationTime(now);

            var updRes = await _repo.UpdateAsync(user, cancellationToken);

            if (!updRes.Succeeded)
                throw new LoginException("Failed to issue token");

            if (roles is null || roles.Count == 0)
                throw new EmptyRolesException("Unable to issue token to user with no valid roles");

            return _tokenService.GenerateAccessToken(roles, tokenUserId);
        }
    }
}
