using MediatR;
using ServerAPIApp.Contracts.Abstractions;
using ServerAPIApp.Core.UseCases.Users;
using ServerAPIApp.Domain.Exceptions.NotFoundExceptions;
using ServerAPIApp.Domain.Exceptions.InternalServerExceptions;

namespace ServerAPIApp.Core.UseCaseHandlers.Users
{
    public class AddUserToRolesCaseHandler : IRequestHandler<AddUserToRolesCase, IEnumerable<string>>
    {
        private IUserRepository _repo;

        //TODO: move to config file
        private const string DefaultRole = "user";

        public AddUserToRolesCaseHandler(IUserRepository repo)
        {
            _repo = repo;
        }

        public async Task<IEnumerable<string>> Handle(AddUserToRolesCase command, CancellationToken cancellationToken)
        {
            var desiredRoles = command.RolesToAdd.Select(r => r.ToLowerInvariant());

            var (user, existingRoles) = await _repo.GetByIdWithRolesAsync(command.UserId, cancellationToken);

            if (user is null)
                throw new ResourceNotFoundException("Resource not found");

            //highly unlikely, but anyway will keep this as fallback logic
            if (existingRoles is null)
            {
                existingRoles = [];
            }

            if (existingRoles.Count == 0 || !existingRoles.Contains(DefaultRole))
            {
                desiredRoles = desiredRoles.Append(DefaultRole);
            }

            var rolesToAdd = desiredRoles.Except(existingRoles);

            if (!rolesToAdd.Any())
                throw new RoleUpdateException("Roles were not provided");

            var res = await _repo.AddToRolesAsync(user, rolesToAdd, cancellationToken);

            if (!res.Succeeded)
                throw new RoleUpdateException("Failed to update user's roles");

            user.ModifiedAt = DateTimeOffset.UtcNow;
            user.ModifiedBy = command.EditorId;

            var updRes = await _repo.UpdateAsync(user, cancellationToken);

            if (!updRes.Succeeded)
                throw new EntityUpdateException("Failed to update user's data");

            return existingRoles.Union(rolesToAdd);
        }
    }
}
