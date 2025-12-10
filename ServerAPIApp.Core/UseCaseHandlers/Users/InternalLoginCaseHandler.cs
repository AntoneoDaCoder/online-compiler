using MediatR;
using ServerAPIApp.Contracts.Abstractions;
using ServerAPIApp.Core.Abstractions;
using ServerAPIApp.Core.UseCases.Users;
using Shared.Helpers;
using ServerAPIApp.Domain.Exceptions.UnauthorizedExceptions;
using ServerAPIApp.Domain.Exceptions.ForbiddenExceptions;
using ServerAPIApp.Contracts.DTOs;

namespace ServerAPIApp.Core.UseCaseHandlers.Users
{
    public class InternalLoginCaseHandler : IRequestHandler<InternalLoginUserCase, LoginDataDto>
    {
        private IUserRepository _repo;
        private IJwtTokenService _tokenService;

        public InternalLoginCaseHandler(IUserRepository repo, IJwtTokenService service)
        {
            _repo = repo;
            _tokenService = service;
        }

        public async Task<LoginDataDto> Handle(InternalLoginUserCase command, CancellationToken cancellationToken)
        {
            var hashedEmail = CryptoHelpers.ComputeSha256Hex(command.Email);

            var (entity, roles) = await _repo.GetByHashedEmailWithRolesAsync(hashedEmail, cancellationToken);

            if (entity is null)
                throw new LoginException("Invalid credentials");

            var pswdCheckRes = await _repo.CheckPasswordAsync(entity, command.Password, cancellationToken);

            if (!pswdCheckRes)
                throw new LoginException("Invalid credentials");

            var now = DateTimeOffset.UtcNow;

            entity.RefreshToken = _tokenService.CreateNewRefreshToken();
            entity.RefreshTokenExpiryTime = _tokenService.GetTokenExpirationTime(now);

            var updRes = await _repo.UpdateAsync(entity, cancellationToken);

            if (!updRes.Succeeded)
                throw new LoginException("Failed to issue token");

            if (roles is null || roles.Count == 0)
                throw new EmptyRolesException("Unable to issue token to user with no valid roles");

            var accessToken = _tokenService.GenerateAccessToken(roles, entity.Id);

            return LoginDataDto.From(entity.Id, entity.Name, roles, entity.CreatedAt, accessToken);
        }
    }
}
