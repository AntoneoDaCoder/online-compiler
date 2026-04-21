using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServerAPIApp.Contracts.Abstractions;
using ServerAPIApp.Contracts.DTOs.Problems;
using ServerAPIApp.Core.UseCases.Problems;
using ServerAPIApp.Extensions;
using ServerAPIApp.Helpers;

namespace ServerAPIApp.Controllers
{
    [Route("api")]
    [ApiController]
    public class ProblemController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly INotificationService _notifier;

        public ProblemController(IMediator mediator, INotificationService notifier)
        {
            _mediator = mediator;
            _notifier = notifier;
        }

        [Authorize(Policy = "DefaultAccess")]
        [HttpGet("problems")]
        public async Task<IActionResult> GetFilteredProblemsAsync([FromQuery] bool? getDeleted = null, [FromQuery] bool? includeLatestVersion = null,
           [FromQuery] bool? includeLanguages = null, CancellationToken cancellationToken = default)
        {
            var userRoles = User.GetRoles();

            var command = new GetFilteredProblemsCase(userRoles, getDeleted, includeLatestVersion, includeLanguages);

            var data = await _mediator.Send(command, cancellationToken);

            return StatusCode(200, data);
        }

        [Authorize(Policy = "EditorAccess")]
        [HttpGet("problem-slug")]
        public async Task<IActionResult> GenerateTaskSlugAsync(CancellationToken cancellationToken = default)
        {
            var command = new GenerateTaskSlugCase();

            var slug = await _mediator.Send(command, cancellationToken);

            return Ok(slug);
        }

        [Authorize(Policy = "EditorAccess")]
        [HttpPost("problems")]
        public async Task<IActionResult> CreateProblemAsync([FromBody] ProblemUpdateDto dto, CancellationToken cancellationToken = default)
        {
            var userId = IdExtractionHelper.GetIdFromJwtToken(HttpContext);

            var command = new CreateProblemCase(userId, dto.Title, dto.Slug);

            var data = await _mediator.Send(command, cancellationToken);

            await _notifier.NotifyGroupAsync("Editors", "TaskCreated", data, cancellationToken);

            return Created();
        }

        [Authorize(Policy = "EditorAccess")]
        [HttpPatch("problems/{id:guid}")]
        public async Task<IActionResult> UpdateProblemAsync([FromRoute] Guid id, [FromBody] ProblemUpdateDto dto, CancellationToken cancellationToken = default)
        {
            var userId = IdExtractionHelper.GetIdFromJwtToken(HttpContext);

            var command = new UpdateProblemCase(id, userId, dto.Title, dto.Slug);

            var data = await _mediator.Send(command, cancellationToken);

            //TODO: notify ALL users about problem update

            return StatusCode(200, data);
        }

        //if this method returns 404, on the client we shouldn't show any message just silently make an empty copy
        [Authorize(Policy = "EditorAccess")]
        [HttpGet("problems/{problemSlug}/latest-version")]
        public async Task<IActionResult> GetLatestVersionBySlugAsync([FromRoute] string problemSlug, CancellationToken cancellationToken = default)
        {
            var command = new GetProblemLatestVersionCase(problemSlug);

            var data = await _mediator.Send(command, cancellationToken);

            return StatusCode(200, data);
        }

        [Authorize(Policy = "EditorAccess")]
        [HttpPost("problems/{problemId:guid}/deletion-requests")]
        public async Task<IActionResult> CreateProblemDeletionRequestAsync([FromRoute] Guid problemId, [FromBody] CreateProblemDeletionRequestDto dto,
            CancellationToken cancellationToken = default)
        {
            var request = new CreateProblemDeletionRequestCase(problemId, dto.InitiatorId, dto.Reason);

            var createdRequest = await _mediator.Send(request, cancellationToken);

            await _notifier.NotifyGroupAsync("Admins", "RequestCreated", createdRequest, cancellationToken);

            return Created();
        }

        [Authorize(Policy = "EditorAccess")]
        [HttpDelete("problems/{problemId:guid}/deletion-requests/{requestId:guid}")]
        public async Task<IActionResult> CancelDeletionRequestAsync([FromRoute] Guid requestId, CancellationToken cancellationToken = default)
        {
            var senderId = IdExtractionHelper.GetIdFromJwtToken(HttpContext);

            var userRoles = User.GetRoles();

            var command = new CancelProblemDeletionRequestCase(senderId, requestId, userRoles);

            var initiatorId = await _mediator.Send(command, cancellationToken);

            await _notifier.NotifyGroupAsync("Admins", "RequestDeleted", requestId, cancellationToken);

            if (initiatorId is not null)
                await _notifier.NotifyUserAsync(initiatorId.ToString()!, "RequestDeleted", requestId, cancellationToken);

            return NoContent();
        }

        [Authorize(Policy = "AdminAccess")]
        [HttpPost("problems/{problemId:guid}/deletion-requests/approved")]
        public async Task<IActionResult> ApproveDeletionRequestAsync([FromRoute] Guid problemId, [FromBody] CancelProblemDeletionRequestDto dto,
            CancellationToken cancellationToken = default)
        {
            var command = new ApproveProblemDeletionRequestCase(dto.RequestId);

            await _mediator.Send(command, cancellationToken);

            await _notifier.NotifyGroupAsync("Editors", "ProblemDeleted", problemId, cancellationToken);

            await _notifier.NotifyGroupAsync("Admins", "RequestApproved", dto.RequestId, cancellationToken);

            return Ok();
        }

        [Authorize(Policy = "AdminAccess")]
        [HttpPatch("problems/deleted/{id:guid}")]
        public async Task<IActionResult> RestoreDeletedProblemAsync([FromRoute] Guid id, CancellationToken cancellationToken = default)
        {
            var command = new RestoreProblemCase(id);

            var restoredProblem = await _mediator.Send(command, cancellationToken);

            await _notifier.NotifyGroupAsync("Users", "ProblemRestored", restoredProblem, cancellationToken);

            return Ok();
        }

        [Authorize(Policy = "EditorAccess")]
        [HttpGet("users/{userId:guid}/deletion-requests")]
        public async Task<IActionResult> GetOwnDeletionRequestsAsync([FromRoute] Guid userId, CancellationToken cancellationToken = default)
        {
            var request = new GetFilteredProblemDeletionRequestsCase(OnlyUser: true, UserId: userId);

            var data = await _mediator.Send(request, cancellationToken);

            return Ok(data);
        }


        [Authorize(Policy = "AdminAccess")]
        [HttpGet("deletion-requests")]
        public async Task<IActionResult> GetProblemDeletionRequestsAsync([FromQuery] bool? excludeUser = null, [FromQuery] Guid? userId = null,
            [FromQuery] bool? exactMatch = null, [FromQuery] Guid? problemId = null, [FromQuery] bool? onlyNotApproved = null, CancellationToken cancellationToken = default)
        {
            var request = new GetFilteredProblemDeletionRequestsCase(ExcludeUser: excludeUser, UserId: userId, ExactMatch: exactMatch,
                ProblemId: problemId, OnlyNotApproved: onlyNotApproved);

            var data = await _mediator.Send(request, cancellationToken);

            return Ok(data);
        }
    }
}
