using MediatR;
using ServerAPIApp.Contracts.Abstractions;
using ServerAPIApp.Core.UseCases.Users;
using ServerAPIApp.Domain.Exceptions;

namespace ServerAPIApp.Core.UseCaseHandlers.Users
{
    public class LogoutUserCaseHandler : IRequestHandler<LogoutUserCase>
    {
        private IUserRepository _repo;

        public LogoutUserCaseHandler(IUserRepository repo)
        {
            _repo = repo;
        }

        public async Task Handle(LogoutUserCase command, CancellationToken cancellationToken)
        {
            if (command.TokenUserId != command.InitiatorId)
                throw new RestrictedActionException("Failed to logout, token mismatch detected");

            var user = await _repo.GetByIdAsync(command.TokenUserId, cancellationToken);

            if (user is null)
                throw new ResourceNotFoundException("Resource not found");

            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = null;

            var updRes = await _repo.UpdateAsync(user, cancellationToken);

            if (!updRes.Succeeded)
                throw new EntityUpdateException("Failed to update user's data");
        }
    }
}
