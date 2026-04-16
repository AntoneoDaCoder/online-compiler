using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServerAPIApp.Contracts.Abstractions;
using ServerAPIApp.Contracts.DTOs.Problems;
using ServerAPIApp.Core.UseCases.ProblemVersions;
using ServerAPIApp.Helpers;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ServerAPIApp.Controllers
{
    [Route("api")]
    [ApiController]
    public class VersionController : ControllerBase
    {
        private IMediator _mediator;
        private INotificationService _notifier;

        public VersionController(IMediator mediator, INotificationService service)
        {
            _mediator = mediator;
            _notifier = service;
        }


        [Authorize(Policy = "EditorAccess")]
        [HttpGet("versions")]
        public async Task<IActionResult> GetVersionsAsync(CancellationToken cancellationToken = default)
        {
            var command = new GetFilteredVersionsCase();

            var data = await _mediator.Send(command, cancellationToken);

            return StatusCode(200, data);
        }

        [Authorize(Policy = "DefaultAccess")]
        [HttpGet("versions/{id:guid}")]
        public async Task<IActionResult> GetVersionByIdAsync([FromRoute] Guid id, CancellationToken cancellationToken = default)
        {
            var command = new GetPublishedByIdCase(id);

            var data = await _mediator.Send(command, cancellationToken);

            return StatusCode(200, data);
        }


        [Authorize(Policy = "EditorAccess")]
        [HttpGet("versions/{id:guid}/as-editor")]
        public async Task<IActionResult> GetEditorVersionByIdAsync([FromRoute] Guid id, CancellationToken cancellationToken = default)
        {
            var command = new GetEditorVersionCase(id);

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

            await _notifier.NotifyGroupAsync("Editors", "DraftCreated", data, cancellationToken);

            return StatusCode(201);
        }

        [Authorize(Policy = "EditorAccess")]
        [HttpPatch("problems/{problemId:guid}/versions/{draftId:guid}")]
        public async Task<IActionResult> UpdateVersionDraftAsync([FromBody] ProblemVersionDto dto, [FromRoute] Guid problemId, [FromRoute] Guid draftId, CancellationToken cancellationToken = default)
        {
            var userId = IdExtractionHelper.GetIdFromJwtToken(HttpContext);

            var command = new UpdateVersionDraftCase(draftId, problemId, dto.Statement, dto.TotalTests, dto.TestManifest);

            await _mediator.Send(command, cancellationToken);

            await _notifier.NotifyGroupAsync("Editors", "DraftUpdated", draftId, cancellationToken);

            return StatusCode(200);
        }

        [Authorize(Policy = "EditorAccess")]
        [HttpDelete("problems/{problemId:guid}/versions/{draftId:guid}")]
        public async Task<IActionResult> DeleteVersionDraftAsync([FromRoute] Guid problemId, [FromRoute] Guid draftId, CancellationToken cancellationToken = default)
        {
            var command = new DeleteVersionDraftCase(draftId, problemId);

            await _mediator.Send(command, cancellationToken);

            await _notifier.NotifyGroupAsync("Editors", "DraftDeleted", draftId, cancellationToken);

            return StatusCode(204);
        }

        [Authorize(Policy = "EditorAccess")]
        [HttpPatch("versions/{draftId:guid}")]
        public async Task<IActionResult> PublishDraftAsync([FromRoute] Guid draftId, CancellationToken cancellationToken = default)
        {
            var userId = IdExtractionHelper.GetIdFromJwtToken(HttpContext);

            var command = new PublishVersionDraftCase(draftId, userId);

            await _mediator.Send(command, cancellationToken);

            await _notifier.NotifyGroupAsync("Users", "VersionPublished", draftId, cancellationToken);

            return StatusCode(200);
        }
    }
}
