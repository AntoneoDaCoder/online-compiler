using MediatR;

namespace ServerAPIApp.Core.UseCases.Users
{
    public record AddUserToRolesCase(Guid UserId, Guid EditorId, IEnumerable<string> RolesToAdd) : IRequest<IEnumerable<string>>;
}
