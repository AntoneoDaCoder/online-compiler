using MediatR;
using ServerAPIApp.Contracts.Abstractions;
using ServerAPIApp.Core.UseCases.Users;
using ServerAPIApp.Domain.Exceptions.InternalServerExceptions;
using ServerAPIApp.Domain.Exceptions.NotFoundExceptions;

namespace ServerAPIApp.Core.UseCaseHandlers.Users
{
    public class RemoveUserFromRolesCaseHandler : IRequestHandler<RemoveUserFromRolesCase, IEnumerable<string>>
    {
        private IUserRepository _repo;

        //TODO: move to config file
        private const string DefaultRole = "user";

        public RemoveUserFromRolesCaseHandler(IUserRepository repo)
        {
            _repo = repo;
        }

        public async Task<IEnumerable<string>> Handle(RemoveUserFromRolesCase command, CancellationToken cancellationToken)
        {
            //var desiredRoles = command.RolesToRemove.Select(r => r.ToLowerInvariant());

            //var (user, existingRoles) = await _repo.GetByIdWithRolesAsync(command.UserId, cancellationToken);

            //if (user is null)
            //    throw new ResourceNotFoundException("Resource not found");

            //IEnumerable<string> result;
            ////highly unlikely, but anyway will keep this as fallback logic
            //if (existingRoles is null || existingRoles.Count == 0)
            //{
            //    result = [DefaultRole];

            //    var res = await _repo.AddToRolesAsync(user, result, cancellationToken);

            //    if (!res.Succeeded)
            //        throw new RoleUpdateException("Failed to update user's roles");
            //}
            //else
            //{
            //    var rolesToRemove = desiredRoles.Except([DefaultRole]);

            //    if (!rolesToRemove.Any())
            //        throw new RoleUpdateException("Roles were not provided");

            //    result = existingRoles.Except(rolesToRemove);

            //    if (!result.Contains(DefaultRole))
            //    {
            //        var addRes = await _repo.AddToRolesAsync(user, [DefaultRole], cancellationToken);

            //        if (!addRes.Succeeded)
            //            throw new RoleUpdateException("Failed to update user's roles");

            //        result = result.Append(DefaultRole);
            //    }

            //    var res = await _repo.RemoveFromRolesAsync(user, rolesToRemove, cancellationToken);

            //    if (!res.Succeeded)
            //        throw new RoleUpdateException("Failed to update user's roles");
            //}

            //user.ModifiedAt = DateTimeOffset.UtcNow;
            //user.ModifiedBy = command.EditorId;

            //var updRes = await _repo.UpdateAsync(user, cancellationToken);

            //if (!updRes.Succeeded)
            //    throw new EntityUpdateException("Failed to update user's data");

            return [];
        }
    }
}
