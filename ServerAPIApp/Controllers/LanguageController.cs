using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServerAPIApp.Core.UseCases.Languages;
using System.Globalization;

namespace ServerAPIApp.Controllers
{
    [Route("api")]
    [ApiController]
    public class LanguageController : ControllerBase
    {
        private IMediator _mediator;

        public LanguageController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [Authorize(Policy = "DefaultAccess")]
        [HttpGet("languages")]
        public async Task<IActionResult> GetAllSupportedLanguagesAsync(CancellationToken cancellationToken = default)
        {
            var command = new GetAllSupportedLanguagesCase();

            var data = await _mediator.Send(command, cancellationToken);

            return StatusCode(200, data);
        }

        [Authorize(Policy = "AdminAccess")]
        [HttpPost("languages")]
        public async Task<IActionResult> CreateLanguageAsync([FromBody] CreateLanguageCase command, CancellationToken cancellationToken = default)
        {
            var data = await _mediator.Send(command, cancellationToken);

            //TODO: notify editors and admins that new language has been created

            return StatusCode(201, data);
        }

        [Authorize(Policy = "AdminAccess")]
        [HttpDelete("languages/{id:guid}")]
        public async Task<IActionResult> DeleteLanguageAsync([FromRoute] Guid id, CancellationToken cancellationToken = default)
        {
            var command = new DeleteLanguageCase(id);

            await _mediator.Send(command, cancellationToken);

            //TODO: notify ALL users that language has been deleted

            return StatusCode(204);
        }

        [Authorize(Policy = "AdminAccess")]
        [HttpPatch("languages/{id:guid}")]
        public async Task<IActionResult> UpdateLanguageAsync([FromRoute] Guid id, [FromBody] string code, [FromBody] string displayName,
            CancellationToken cancellationToken = default)
        {
            var command = new UpdateLanguageCase(id, displayName, code);

            var updatedData = await _mediator.Send(command, cancellationToken);

            //TODO: notify ALL users that language has been updated

            return StatusCode(200, updatedData);
        }
    }
}
