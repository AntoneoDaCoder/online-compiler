using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServerAPIApp.Core.UseCases.Users;
using ServerAPIApp.Helpers;

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
        [HttpPost("users/{id:guid}/roles")]
        public async Task<IActionResult> AddUserToRolesAsync([FromBody] IEnumerable<string> addedRoles, [FromRoute] Guid id, CancellationToken cancellationToken = default)
        {
            var editorId = IdExtractionHelper.GetIdFromJwtToken(HttpContext);

            var command = new AddUserToRolesCase(id, editorId, addedRoles);

            var updatedRoles = await _mediator.Send(command, cancellationToken);

            //TODO: notify user and admins about role change

            return StatusCode(201, updatedRoles);
        }

        [Authorize(Policy = "AdminAccess")]
        [HttpDelete("users/{id:guid}/roles")]
        public async Task<IActionResult> RemoveUserFromRolesAsync([FromBody] IEnumerable<string> removedRoles, [FromRoute] Guid id, CancellationToken cancellationToken = default)
        {
            var editorId = IdExtractionHelper.GetIdFromJwtToken(HttpContext);

            var command = new RemoveUserFromRolesCase(id, editorId, removedRoles);

            var updatedRoles = await _mediator.Send(command, cancellationToken);

            //TODO: notify user and admins about role change

            return StatusCode(200, updatedRoles);
        }

        [Authorize(Policy = "DefaultAccess")]
        [HttpPost("users")]
        public async Task<IActionResult> SyncExternalAccountAsync([FromBody] string id, CancellationToken cancellationToken = default)
        {
            var command = new SyncExternalAccountCase(id);

            await _mediator.Send(command, cancellationToken);

            return Ok();
        }
    }
}
