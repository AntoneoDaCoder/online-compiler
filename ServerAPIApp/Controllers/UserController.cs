using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServerAPIApp.Contracts.Abstractions;
using ServerAPIApp.Contracts.DTOs.Auth;
using ServerAPIApp.Core.UseCases.Users;
using ServerAPIApp.Helpers;

namespace ServerAPIApp.Controllers
{
    [Route("api")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private IMediator _mediator;
        private readonly INotificationService _notifier;

        public UserController(IMediator mediator, INotificationService notifier)
        {
            _mediator = mediator;
            _notifier = notifier;
        }

        [Authorize(Policy = "DefaultAccess")]
        [HttpPost("users")]
        public async Task<IActionResult> SyncExternalAccountAsync([FromBody] string id, CancellationToken cancellationToken = default)
        {
            var command = new SyncExternalAccountCase(id);

            await _mediator.Send(command, cancellationToken);

            return Ok();
        }

        [Authorize(Policy = "AdminAccess")]
        [HttpGet("users")]
        public async Task<IActionResult> GetUsersAsync(CancellationToken cancellationToken = default)
        {
            var request = new GetUsersCase();

            var users = await _mediator.Send(request, cancellationToken);

            return Ok(users);
        }

        [Authorize(Policy = "AdminAccess")]
        [HttpGet("users/{userId}")]
        public async Task<IActionResult> GetUsersRealmRolesAsync([FromRoute] string userId, CancellationToken cancellationToken = default)
        {
            var request = new GetUsersClientRolesCase(userId);

            var roles = await _mediator.Send(request, cancellationToken);

            return Ok(roles);
        }

        [Authorize(Policy = "AdminAccess")]
        [HttpGet("roles")]
        public async Task<IActionResult> GetAvailableRolesAsync(CancellationToken cancellationToken = default)
        {
            var request = new GetAvailableRolesCase();

            var roles = await _mediator.Send(request, cancellationToken);

            return Ok(roles);
        }

        [Authorize(Policy = "AdminAccess")]
        [HttpPatch("user/{userId}/roles")]
        public async Task<IActionResult> UpdateRolesAsync([FromRoute] string userId, [FromBody] UpdateRolesDto dto, CancellationToken cancellationToken = default)
        {
            var command = UpdateUserRolesCase.From(userId, dto);

            await _mediator.Send(command, cancellationToken);

            await _notifier.NotifyUserAsync(userId, "RolesUpdated", dto, cancellationToken);

            return Ok();
        }

        [Authorize(Policy = "DefaultAccess")]
        [HttpDelete("users/{userId}")]
        public async Task<IActionResult> SoftDeleteUserAsync([FromRoute] string userId, CancellationToken cancellationToken = default)
        {
            var senderId = IdExtractionHelper.GetIdFromJwtToken(HttpContext);

            var command = new SoftDeleteAccountCase(userId, senderId);

            var data = await _mediator.Send(command, cancellationToken);

            return Ok(data);
        }

        [Authorize(Policy = "DefaultAccess")]
        [HttpPatch("users/{userId}")]
        public async Task<IActionResult> CancelAccountDeletionAsync([FromRoute] string userId, CancellationToken cancellationToken = default)
        {
            var senderId = IdExtractionHelper.GetIdFromJwtToken(HttpContext);

            var command = new CancelAccountDeletionCase(userId, senderId);

            await _mediator.Send(command, cancellationToken);

            return Ok();
        }

        [Authorize(Policy = "DefaultAccess")]
        [HttpGet("users/{userId}/metadata")]
        public async Task<IActionResult> GetUserMetadataAsync([FromRoute] string userId, CancellationToken cancellationToken = default)
        {
            var senderId = IdExtractionHelper.GetIdFromJwtToken(HttpContext);

            var request = new GetUserMetadataCase(userId, senderId);

            var metadata = await _mediator.Send(request, cancellationToken);

            return Ok(metadata);
        }
    }
}
