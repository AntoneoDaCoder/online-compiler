using MediatR;
using ServerAPIApp.Core.Abstractions;
using ServerAPIApp.Core.UseCases.Users;

namespace ServerAPIApp.Core.UseCaseHandlers.Users
{
    public class UpdateRolesCaseHandler : IRequestHandler<UpdateUserRolesCase>
    {
        private readonly IExternalAuthService _service;

        public UpdateRolesCaseHandler(IExternalAuthService service)
        {
            _service = service;
        }

        public async Task Handle(UpdateUserRolesCase command, CancellationToken cancellationToken)
        {
            await _service.UpdateUserRolesAsync(command.UserId, command.RolesToRemove, command.RolesToAdd, cancellationToken);
        }
    }
}
