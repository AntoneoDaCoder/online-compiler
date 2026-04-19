using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServerAPIApp.Core.UseCases.Problems;
using ServerAPIApp.Extensions;
using ServerAPIApp.Helpers;
using ServerAPIApp.Contracts.DTOs.Problems;

namespace ServerAPIApp.Controllers
{
    [Route("api")]
    [ApiController]
    public class ProblemController : ControllerBase
    {
        private IMediator _mediator;

        public ProblemController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [Authorize(Policy = "DefaultAccess")]
        [HttpGet("problems")]
        public async Task<IActionResult> GetProblemsAsync(CancellationToken cancellationToken = default)
        {
            var userRoles = User.GetRoles();

            var command = new GetFilteredProblemsCase(userRoles);

            var data = await _mediator.Send(command, cancellationToken);

            return StatusCode(200, data);
        }

        [Authorize(Policy = "EditorAccess")]
        [HttpPost("problems")]
        public async Task<IActionResult> CreateProblemAsync([FromBody] ProblemUpdateDto dto, CancellationToken cancellationToken = default)
        {
            var userId = IdExtractionHelper.GetIdFromJwtToken(HttpContext);

            var command = new CreateProblemCase(userId, dto.Title, dto.Slug);

            var data = await _mediator.Send(command, cancellationToken);

            //TODO: notify editors and admins about problem creation

            return StatusCode(200, data);
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
            Console.WriteLine("[API] got slug: " + problemSlug);

            var command = new GetProblemLatestVersionCase(problemSlug);

            var data = await _mediator.Send(command, cancellationToken);

            Console.WriteLine("[API] about to return data");

            return StatusCode(200, data);
        }

        [Authorize(Policy = "EditorAccess")]
        [HttpPost("problems/{problemId:guid}/deletion-requests")]
        public async Task<IActionResult> CreateProblemDeletionRequestAsync([FromRoute] Guid problemId, [FromBody] CreateProblemDeletionRequestDto dto,
            CancellationToken cancellationToken = default)
        {
            var request = new CreateProblemDeletionRequestCase(problemId, dto.InitiatorId, dto.Reason);

            await _mediator.Send(request, cancellationToken);

            return Created();
        }

        [Authorize(Policy = "EditorAccess")]
        [HttpDelete("problems/{problemId:guid}/deletion-requests/{requestId:guid}")]
        public async Task<IActionResult> CancelDeletionRequestAsync([FromRoute] Guid requestId, CancellationToken cancellationToken = default)
        {
            var senderId = IdExtractionHelper.GetIdFromJwtToken(HttpContext);

            var userRoles = User.GetRoles();

            var command = new CancelProblemDeletionRequestCase(senderId, requestId, userRoles);

            await _mediator.Send(command, cancellationToken);

            return NoContent();
        }

        [Authorize(Policy = "AdminAccess")]
        [HttpPost("problems/{problemId:guid}/deletion-requests/approved")]
        public async Task<IActionResult> ApproveDeletionRequestAsync([FromBody] Guid requestId, CancellationToken cancellationToken = default)
        {
            var command = new ApproveProblemDeletionRequestCase(requestId);

            await _mediator.Send(command, cancellationToken);

            return Ok();
        }

        [Authorize(Policy = "AdminAccess")]
        [HttpPatch("problems/deleted/{id:guid}")]
        public async Task<IActionResult> RestoreDeletedProblemAsync([FromRoute] Guid id, CancellationToken cancellationToken = default)
        {
            var command = new RestoreProblemCase(id);

            await _mediator.Send(command, cancellationToken);

            return Ok();
        }

        [Authorize(Policy = "EditorAccess")]
        [HttpGet("users/{userId:guid}/deletion-requests")]
        public async Task<IActionResult> GetPagedOwnDeletionRequestsAsync([FromRoute] Guid userId, CancellationToken cancellationToken = default)
        {
            var request = new GetFilteredProblemDeletionRequestsCase(x => x.InitiatorId == userId/*, page, pageSize*/);

            var data = await _mediator.Send(request, cancellationToken);

            return Ok(data);
        }


        [Authorize(Policy = "AdminAccess")]
        [HttpGet("deletion-requests")]
        public async Task<IActionResult> GetPagedProblemDeletionRequestsAsync(CancellationToken cancellationToken = default)
        {
            var userId = IdExtractionHelper.GetIdFromJwtToken(HttpContext);

            var request = new GetFilteredProblemDeletionRequestsCase(x => x.InitiatorId != userId/*, page, pageSize*/);

            var data = await _mediator.Send(request, cancellationToken);

            return Ok(data);
        }
    }
}
