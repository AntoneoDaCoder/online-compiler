using MediatR;
using ServerAPIApp.Contracts.DTOs.Auth;
using ServerAPIApp.Core.Abstractions;
using ServerAPIApp.Core.UseCases.Users;

namespace ServerAPIApp.Core.UseCaseHandlers.Users
{
    public class GetUsersCaseHandler : IRequestHandler<GetUsersCase, IEnumerable<ExternalUserResponseDto>?>
    {
        private readonly IExternalAuthService _service;

        public GetUsersCaseHandler(IExternalAuthService svc)
        {
            _service = svc;
        }

        public async Task<IEnumerable<ExternalUserResponseDto>?> Handle(GetUsersCase request, CancellationToken cancellationToken)
        {
            var users = await _service.GetAllUsersAsync(cancellationToken);

            return users;
        }
    }
}
