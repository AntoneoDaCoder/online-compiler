using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServerAPIApp.Core.UseCases.Users;

namespace ServerAPIApp.Controllers
{
    [Route("api")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private IMediator _mediator;

        public UserController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [Authorize(Policy = "AdminAccess")]
        [HttpPost("users/user/roles")]
        public async Task<IActionResult> AddUserToRolesAsync([FromBody] AddUserToRolesCase dto, CancellationToken cancellationToken = default)
        {
            var updatedRoles = await _mediator.Send(dto, cancellationToken);

            //TODO: notify user and admins about role change

            return StatusCode(201, updatedRoles);
        }

        [Authorize(Policy = "AdminAccess")]
        [HttpDelete("users/user/roles")]
        public async Task<IActionResult> AddUserToRolesAsync([FromBody] RemoveUserFromRolesCase dto, CancellationToken cancellationToken = default)
        {
            var updatedRoles = await _mediator.Send(dto, cancellationToken);

            //TODO: notify user and admins about role change

            return StatusCode(200, updatedRoles);
        }
    }
}
