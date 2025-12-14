using MediatR;
using ServerAPIApp.Contracts.Abstractions;
using ServerAPIApp.Contracts.DTOs;
using ServerAPIApp.Core.Abstractions;
using ServerAPIApp.Core.UseCases.Users;
using ServerAPIApp.Domain.Entities;
using ServerAPIApp.Domain.Exceptions.ConflictExceptions;
using ServerAPIApp.Domain.Exceptions.UnauthorizedExceptions;
using Shared.Helpers;
using System.Xml.Linq;

namespace ServerAPIApp.Core.UseCaseHandlers.Users
{
    public class RegisterCaseHandler : IRequestHandler<RegisterUserCase, LoginDataDto>
    {
        private IUserRepository _repo;
        private IJwtTokenService _tokenService;
        private ISecretProtector _protector;


        //TODO: move it outta here later
        private const string DefaultRole = "user";

        public RegisterCaseHandler(IUserRepository repo, IJwtTokenService service, ISecretProtector protector)
        {
            _repo = repo;
            _tokenService = service;
            _protector = protector;
        }

        public async Task<LoginDataDto> Handle(RegisterUserCase command, CancellationToken cancellationToken)
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
                CreatedBy = Guid.Empty,
                Name = command.Name,
                EmailHash = emailHash,
                EncryptedEmail = _protector.Protect(command.Email),
                RefreshToken = _tokenService.CreateNewRefreshToken(),
                RefreshTokenExpiryTime = _tokenService.GetTokenExpirationTime(now),

                SecurityStamp = newId.ToString(),
                ConcurrencyStamp = newId.ToString(),
                UserName = command.Name,
                Email = newId.ToString(),
                NormalizedUserName = newId.ToString().ToUpperInvariant(),
                NormalizedEmail = newId.ToString().ToUpperInvariant(),
                EmailConfirmed = true,
                LockoutEnabled = true,
                AccessFailedCount = 0,
                TwoFactorEnabled = false,
                PhoneNumberConfirmed = false,
            };

            var res = await _repo.CreateAsync(newUser, command.Password, cancellationToken);

            if (!res.Succeeded)
                throw new RegistrationException("Failed to create user's account");

            var roleRes = await _repo.AddToRolesAsync(newUser, [DefaultRole], cancellationToken);

            if (!roleRes.Succeeded)
                throw new RegistrationException("Failed to add user to role");

            var accessToken = _tokenService.GenerateAccessToken([DefaultRole], newId);

            return LoginDataDto.From(newId, command.Name, [DefaultRole], newUser.CreatedAt, accessToken);
        }
    }
}
