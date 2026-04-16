using MediatR;
using ServerAPIApp.Contracts.Abstractions;
using ServerAPIApp.Core.UseCases.Users;
using ServerAPIApp.Domain.Entities;

namespace ServerAPIApp.Core.UseCaseHandlers.Users
{
    public class SyncExternalAccountCaseHandler : IRequestHandler<SyncExternalAccountCase>
    {
        private readonly IUserRepository _repo;

        public SyncExternalAccountCaseHandler(IUserRepository repo)
        {
            _repo = repo;
        }

        public async Task Handle(SyncExternalAccountCase command, CancellationToken cancellationToken)
        {
            var id = Guid.Parse(command.ProviderId);

            var entity = await _repo.GetByIdAsync(id, cancellationToken);

            if (entity is null)
            {
                var newAccount = new UserEntity()
                {
                    Id = id,
                    ExternalProviderId = command.ProviderId
                };

                await _repo.CreateAsync(newAccount, cancellationToken);
            }
        }
    }
}
