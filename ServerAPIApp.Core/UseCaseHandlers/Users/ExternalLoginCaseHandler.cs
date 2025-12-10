using MediatR;
using ServerAPIApp.Contracts.Abstractions;
using ServerAPIApp.Core.Abstractions;
using ServerAPIApp.Core.UseCases.Users;
using ServerAPIApp.Domain.Entities;
using ServerAPIApp.Domain.Exceptions.UnauthorizedExceptions;
using ServerAPIApp.Domain.Exceptions.ForbiddenExceptions;
using Shared.Helpers;
using ServerAPIApp.Contracts.DTOs;

namespace ServerAPIApp.Core.UseCaseHandlers.Users
{
    public class ExternalLoginCaseHandler : IRequestHandler<ExternalLoginUserCase, LoginDataDto>
    {
        private IUserRepository _repo;
        private ISecretProtector _protector;
        private IJwtTokenService _tokenService;
        private IGoogleAuthTokenValidator _authTokenValidator;

        //TODO: move to config file
        private const string ProviderName = "Google";
        private const string DefaultRole = "user";

        public ExternalLoginCaseHandler(IUserRepository repo, ISecretProtector protector, IJwtTokenService service, IGoogleAuthTokenValidator validator)
        {
            _repo = repo;
            _protector = protector;
            _tokenService = service;
            _authTokenValidator = validator;
        }

        public async Task<LoginDataDto> Handle(ExternalLoginUserCase command, CancellationToken cancellationToken)
        {
            var payload = await _authTokenValidator.ValidateTokenAsync(command.IdToken, cancellationToken);

            if (payload is null)
                throw new LoginException("Invalid Google token");

            var providerKey = payload.Subject;

            var (googleUser, roles) = await _repo.GetByLoginWithRolesAsync(ProviderName, providerKey, cancellationToken);

            var now = DateTimeOffset.UtcNow;

            if (googleUser is not null)
            {
                if (roles is null || roles.Count == 0)
                    throw new EmptyRolesException("Unable to issue token to user with no valid roles");

                googleUser.RefreshToken = _tokenService.CreateNewRefreshToken();
                googleUser.RefreshTokenExpiryTime = _tokenService.GetTokenExpirationTime(now);

                var updRes = await _repo.UpdateAsync(googleUser, cancellationToken);

                if (!updRes.Succeeded)
                    throw new LoginException("Failed to issue token");
            }
            else
            {
                var hashedEmail = CryptoHelpers.ComputeSha256Hex(payload.Email);

                var (internalUser, internalRoles) = await _repo.GetByHashedEmailWithRolesAsync(hashedEmail, cancellationToken);

                if (internalUser is null)
                {
                    var newId = Guid.NewGuid();
                    var newPswd = Guid.NewGuid();

                    googleUser = new UserEntity()
                    {
                        Id = newId,
                        CreatedAt = DateTimeOffset.UtcNow,
                        CreatedBy = Guid.Empty,
                        Name = payload.Name,
                        EmailHash = hashedEmail,
                        EncryptedEmail = _protector.Protect(payload.Email),
                        RefreshToken = _tokenService.CreateNewRefreshToken(),
                        RefreshTokenExpiryTime = _tokenService.GetTokenExpirationTime(now),
                    };

                    var res = await _repo.CreateAsync(googleUser, newPswd.ToString(), cancellationToken);

                    if (!res.Succeeded)
                        throw new RegistrationException("Failed to create user's account");

                    var roleRes = await _repo.AddToRolesAsync(googleUser, [DefaultRole], cancellationToken);

                    if (!roleRes.Succeeded)
                        throw new RegistrationException("Failed to add user to role");

                    roles = [DefaultRole];

                    var linked = await _repo.AddLoginAsync(googleUser, ProviderName, providerKey, cancellationToken: cancellationToken);

                    if (!linked)
                        throw new RegistrationException("Failed to link internal account to provider");
                }
                else
                {
                    googleUser = internalUser;

                    if (internalRoles is null || internalRoles.Count == 0)
                        throw new EmptyRolesException("Unable to issue token to user with no valid roles");

                    roles = internalRoles;

                    googleUser.RefreshToken = _tokenService.CreateNewRefreshToken();
                    googleUser.RefreshTokenExpiryTime = _tokenService.GetTokenExpirationTime(now);

                    var updRes = await _repo.UpdateAsync(googleUser, cancellationToken);

                    if (!updRes.Succeeded)
                        throw new LoginException("Failed to issue token");

                    var linked = await _repo.AddLoginAsync(googleUser, ProviderName, providerKey, cancellationToken: cancellationToken);

                    if (!linked)
                        throw new LoginException("Failed to link internal account to provider");
                }
            }

            var accessToken = _tokenService.GenerateAccessToken(roles, googleUser.Id);

            return LoginDataDto.From(googleUser.Id, googleUser.Name, roles, googleUser.CreatedAt, accessToken);
        }
    }
}
