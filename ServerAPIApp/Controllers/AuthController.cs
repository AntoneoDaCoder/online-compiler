using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServerAPIApp.Core.UseCases.Users;
using ServerAPIApp.Extensions;
using ServerAPIApp.Helpers;

namespace ServerAPIApp.Controllers
{
    [ApiController]
    [Route("api")]
    public class AuthController : ControllerBase
    {
        private IMediator _mediator;

        public AuthController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [AllowAnonymous]
        [HttpPost("auth/login/internal")]
        public async Task<IActionResult> InternalLoginUserAsync([FromBody] InternalLoginUserCase request, CancellationToken cancellationToken = default)
        {
            var accessToken = await _mediator.Send(request, cancellationToken);

            return StatusCode(200, accessToken);
        }

        [AllowAnonymous]
        [HttpPost("auth/register")]
        public async Task<IActionResult> RegisterUserAsync([FromBody] RegisterUserCase request, CancellationToken cancellationToken = default)
        {
            var accessToken = await _mediator.Send(request, cancellationToken);

            return StatusCode(201, accessToken);
        }

        [Authorize(Policy = "DefaultAccess")]
        [HttpPost("auth/token/refresh")]
        public async Task<IActionResult> RefreshAccessTokenAsync(CancellationToken cancellationToken = default)
        {
            var token = HttpContext.GetBearerToken();

            var userId = IdExtractionHelper.GetIdFromJwtToken(HttpContext);

            var command = new RefreshAccessTokenCase(userId, token);

            var refreshedToken = await _mediator.Send(command, cancellationToken);

            return StatusCode(200, refreshedToken);
        }
    }
}
