using MediatR;
using ServerAPIApp.Contracts.DTOs.Auth;
using ServerAPIApp.Core.Abstractions;
using ServerAPIApp.Core.UseCases.Users;

namespace ServerAPIApp.Core.UseCaseHandlers.Users
{
    public class GetUsersRealmRolesCaseHandler : IRequestHandler<GetUsersRealmRolesCase, IEnumerable<ExternalRoleDto>?>
    {
        private readonly IExternalAuthService _service;

        public GetUsersRealmRolesCaseHandler(IExternalAuthService service)
        {
            _service = service;
        }

        public async Task<IEnumerable<ExternalRoleDto>?> Handle(GetUsersRealmRolesCase request, CancellationToken cancellationToken)
        {
            var roles = await _service.GetUserRealmRolesAsync(request.UserId, cancellationToken);

            return roles;
        }
    }
}
