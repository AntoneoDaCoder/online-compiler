using MediatR;
using ServerAPIApp.Contracts.Abstractions;
using ServerAPIApp.Core.Abstractions;
using ServerAPIApp.Core.UseCases.Users;
using ServerAPIApp.Domain.Entities;
using ServerAPIApp.Domain.Exceptions;
using Shared.Helpers;

namespace ServerAPIApp.Core.UseCaseHandlers.Users
{
    public class RegisterCaseHandler : IRequestHandler<RegisterUserCase, string>
    {
        private IUserRepository _repo;
        private IJwtTokenService _tokenService;
        private ISecretProtector _protector;


        //TODO: move it outta here later
        private const string DefaultRole = "User";

        public RegisterCaseHandler(IUserRepository repo, IJwtTokenService service, ISecretProtector protector)
        {
            _repo = repo;
            _tokenService = service;
            _protector = protector;
        }

        public async Task<string> Handle(RegisterUserCase command, CancellationToken cancellationToken)
        {
            var emailHash = CryptoHelpers.ComputeSha256Hex(command.Email);

            var entity = await _repo.GetByHashedEmailAsync(emailHash, cancellationToken);

            if (entity is not null)
                throw new DuplicateException("User with such email already exists");

            var newId = Guid.NewGuid();

            var now = DateTimeOffset.UtcNow;

            var newUser = new UserEntity()
            {
                Id = newId,
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = newId,
                Name = command.Name,
                EmailHash = emailHash,
                EncryptedEmail = _protector.Protect(command.Email),
                RefreshToken = _tokenService.CreateNewRefreshToken(),
                RefreshTokenExpiryTime = _tokenService.GetTokenExpirationTime(now),
            };

            var res = await _repo.CreateAsync(newUser, command.Password, cancellationToken);

            if (!res.Succeeded)
                throw new RegistrationException("Failed to create user's account");

            var roleRes = await _repo.AddToRoleAsync(newUser, DefaultRole, cancellationToken);

            if (!roleRes.Succeeded)
                throw new RegistrationException("Failed to add user to role");

            return _tokenService.GenerateAccessToken([DefaultRole], newId);
        }
    }
}
