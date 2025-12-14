using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServerAPIApp.Core.UseCases.Problems;
using ServerAPIApp.Contracts.DTOs;
using ServerAPIApp.Extensions;
using ServerAPIApp.Helpers;

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


        [Authorize(Policy = "EditorAccess")]
        [HttpDelete("problems/{id:guid}")]
        public async Task<IActionResult> DeleteProblemAsync([FromRoute] Guid id, CancellationToken cancellationToken = default)
        {
            var userId = IdExtractionHelper.GetIdFromJwtToken(HttpContext);

            var command = new DeleteProblemCase(id, userId);

            await _mediator.Send(command, cancellationToken);

            //TODO: notify ALL users about unlisting a problem

            return StatusCode(204);
        }

        [Authorize(Policy = "EditorAccess")]
        [HttpPatch("problems/deleted/{id:guid}")]
        public async Task<IActionResult> RestoreDeletedProblemAsync([FromRoute] Guid id, CancellationToken cancellationToken = default)
        {
            var userId = IdExtractionHelper.GetIdFromJwtToken(HttpContext);

            var command = new RestoreProblemCase(id, userId);

            //TODO: get problem data
            await _mediator.Send(command, cancellationToken);

            //TODO: notify all users that problem was restored (if it has a version), otherwise only editors and admins

            return StatusCode(201);
        }


        //if this method returns 404, on the client we shouldn't show any message just silently make an empty copy
        [Authorize(Policy = "EditorAccess")]
        [HttpGet("problems/{problemSlug:string}/latest-version")]
        public async Task<IActionResult> GetLatestVersionBySlugAsync([FromRoute] string problemSlug, CancellationToken cancellationToken = default)
        {
            var command = new GetProblemLatestVersionCase(problemSlug);

            var data = await _mediator.Send(command, cancellationToken);

            return StatusCode(200, data);
        }
    }
}
