using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAPIApp.Core.UseCases.Users
{
    public record RemoveUserFromRolesCase(Guid UserId, Guid EditorId, IEnumerable<string> RolesToRemove) : IRequest<IEnumerable<string>>;
}
