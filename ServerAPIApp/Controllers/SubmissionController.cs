using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServerAPIApp.Core.UseCases.Submissions;

namespace ServerAPIApp.Controllers
{
    [Route("api")]
    [ApiController]
    public class SubmissionController : ControllerBase
    {
        private IMediator _mediator;

        public SubmissionController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [Authorize(Policy = "DefaultAccess")]
        [HttpGet("user/{userId:guid}/submissions/{id:guid}")]
        public async Task<IActionResult> GetSubmissionByIdAsync([FromRoute] Guid id, CancellationToken cancellationToken = default)
        {
            var command = new GetSubmissionByIdCase(id);

            var data = await _mediator.Send(command, cancellationToken);

            return StatusCode(200, data);
        }

        [Authorize(Policy = "DefaultAccess")]
        [HttpDelete("user/{userId:guid}/submissions/{id:guid}")]
        public async Task<IActionResult> DeleteSubmissionByIdAsync([FromRoute] Guid id, CancellationToken cancellationToken = default)
        {
            var command = new DeleteSubmissionCase(id);

            await _mediator.Send(command, cancellationToken);

            return StatusCode(204);
        }

        [Authorize(Policy = "DefaultAccess")]
        [HttpGet("user/{userId:guid}/submissions")]
        public async Task<IActionResult> GetFilteredUserSubmissionsAsync([FromRoute] Guid userId, [FromQuery] IEnumerable<string>? languages,
            [FromQuery] bool? isSuccessful, CancellationToken cancellationToken = default)
        {
            var command = new GetFilteredUserSubmissionsCase(userId, languages, isSuccessful);

            var data = await _mediator.Send(command, cancellationToken);

            return StatusCode(200, data);
        }
    }
}
