using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServerAPIApp.Contracts.DTOs;
using ServerAPIApp.Core.UseCases.ProblemVersions;
using ServerAPIApp.Helpers;

namespace ServerAPIApp.Controllers
{
    [Route("api")]
    [ApiController]
    public class VersionController : ControllerBase
    {
        private IMediator _mediator;

        public VersionController(IMediator mediator)
        {
            _mediator = mediator;
        }


        [Authorize(Policy = "EditorAccess")]
        [HttpGet("versions")]
        public async Task<IActionResult> GetVersionsAsync(CancellationToken cancellationToken = default)
        {
            var command = new GetFilteredVersionsCase();

            var data = await _mediator.Send(command, cancellationToken);

            return StatusCode(200, data);
        }


        [Authorize(Policy = "EditorAccess")]
        [HttpPost("problems/{problemId:guid}/versions")]
        public async Task<IActionResult> CreateVersionDraftAsync([FromBody] ProblemVersionDto dto, [FromRoute] Guid problemId, CancellationToken cancellationToken = default)
        {
            var userId = IdExtractionHelper.GetIdFromJwtToken(HttpContext);

            var command = new CreateVersionDraftCase(problemId, userId, dto.Statement, dto.TotalTests, dto.TestManifest);

            var data = await _mediator.Send(command, cancellationToken);

            //TODO: notify editors and admins about draft creation

            return StatusCode(201, data);
        }

        [Authorize(Policy = "EditorAccess")]
        [HttpPatch("problems/{problemId:guid}/versions/{draftId:guid}")]
        public async Task<IActionResult> UpdateVersionDraftAsync([FromBody] ProblemVersionDto dto, [FromRoute] Guid problemId, [FromRoute] Guid draftId, CancellationToken cancellationToken = default)
        {
            var userId = IdExtractionHelper.GetIdFromJwtToken(HttpContext);

            var command = new UpdateVersionDraftCase(draftId, problemId, dto.Statement, dto.TotalTests, dto.TestManifest);

            var data = _mediator.Send(command, cancellationToken);

            //TODO: notify editors and admins about draft update

            return StatusCode(200, data);
        }

        [Authorize(Policy = "EditorAccess")]
        [HttpDelete("problems/{problemId:guid}/versions/{draftId:guid}")]
        public async Task<IActionResult> DeleteVersionDraftAsync([FromRoute] Guid problemId, [FromRoute] Guid draftId, CancellationToken cancellationToken = default)
        {
            var command = new DeleteVersionDraftCase(draftId, problemId);

            await _mediator.Send(command, cancellationToken);

            //TODO: notify editors and admins about draft deletion

            return StatusCode(204);
        }

        [Authorize(Policy = "EditorAccess")]
        [HttpPatch("versions/{draftId:guid}")]
        public async Task<IActionResult> PublishDraftAsync([FromRoute] Guid draftId, CancellationToken cancellationToken = default)
        {
            var userId = IdExtractionHelper.GetIdFromJwtToken(HttpContext);

            var command = new PublishVersionDraftCase(draftId, userId);

            //TODO: get data for USERS and EDITORS
            await _mediator.Send(command, cancellationToken);

            //TODO: notify USERS that problem has been created so problem component updates itself, notify EDITORS that problem has been updated (if it wasn't present create new, otherwise update existing)

            return StatusCode(200);
        }
    }
}
