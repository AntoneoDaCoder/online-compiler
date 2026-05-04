using MediatR;
using ServerAPIApp.Contracts.DTOs.Auth;
using ServerAPIApp.Core.Abstractions;
using ServerAPIApp.Core.UseCases.Users;

namespace ServerAPIApp.Core.UseCaseHandlers.Users
{
    public class GetUsersClientRolesCaseHandler : IRequestHandler<GetUsersClientRolesCase, IEnumerable<ExternalRoleDto>?>
    {
        private readonly IExternalAuthService _service;

        public GetUsersClientRolesCaseHandler(IExternalAuthService service)
        {
            _service = service;
        }

        public async Task<IEnumerable<ExternalRoleDto>?> Handle(GetUsersClientRolesCase request, CancellationToken cancellationToken)
        {
            var roles = await _service.GetUserClientRolesAsync(request.UserId, cancellationToken);

            return roles;
        }
    }
}
