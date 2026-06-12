using MediatR;
using ServerAPIApp.Contracts.DTOs.Auth;
using ServerAPIApp.Core.Abstractions;
using ServerAPIApp.Core.UseCases.Users;

namespace ServerAPIApp.Core.UseCaseHandlers.Users
{
    public class GetAvailableRolesCaseHandler : IRequestHandler<GetAvailableRolesCase, IEnumerable<ExternalRoleDto>?>
    {
        private readonly IExternalAuthService _service;

        public GetAvailableRolesCaseHandler(IExternalAuthService service)
        {
            _service = service;
        }

        public async Task<IEnumerable<ExternalRoleDto>?> Handle(GetAvailableRolesCase request, CancellationToken cancellationToken)
        {
            var roles = await _service.GetAvailableClientRolesAsync(cancellationToken);

            return roles;
        }
    }
}
