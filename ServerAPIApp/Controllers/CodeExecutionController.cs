using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServerAPIApp.Contracts.Abstractions;
using ServerAPIApp.Contracts.DTOs;
using ServerAPIApp.Core.UseCases.Submissions;
using ServerAPIApp.Helpers;
using Shared.DTOs;
using Shared.Enums;

namespace ServerAPIApp.Controllers
{
    [ApiController]
    [Route("api")]
    public class CodeExecutionController : ControllerBase
    {
        private ICodeDispatcher _dispatcher;
        private ISubmissionNotifier _notifier;
        private IMediator _mediator;

        public CodeExecutionController(ICodeDispatcher dispatcher, ISubmissionNotifier notifier, IMediator mediator)
        {
            _dispatcher = dispatcher;
            _notifier = notifier;
            _mediator = mediator;
        }

        [Authorize(Policy = "DefaultAccess")]
        [HttpPost("jobs/start")]
        public async Task<IActionResult> ScheduleCodeExecutionAsync([FromBody] CodeRequestDto dto, CancellationToken cancellationToken)
        {
            try
            {
                var userId = IdExtractionHelper.GetIdFromJwtToken(HttpContext);

                await _dispatcher.ScheduleForExecutionAsync(userId, dto, cancellationToken);

                Console.WriteLine($"[API Controller] Received a request [Id:{dto.RequestId}], server time: {DateTime.Now}");

                var response = new CodeResponseDto()
                {
                    RequestId = dto.RequestId,
                    Status = RequestStatus.Acknowledged,
                    Language = dto.LanguageCode,
                    Result = new ExecutionResultDto()
                    {
                        Status = ExecutionStatus.Pending,
                        RequestSentAt = dto.RequestSentAt
                    }
                };

                return StatusCode(202, response);
            }
            catch (Exception ex)
            {
                var response = new CodeResponseDto()
                {
                    RequestId = dto.RequestId,
                    Status = RequestStatus.Failed,
                    Language = dto.LanguageCode,
                    Result = new ExecutionResultDto()
                    {
                        Status = ExecutionStatus.FailedToExecute,
                        ExitCode = -1,
                        ConsoleOutput = ex.Message,
                        RequestSentAt = dto.RequestSentAt
                    }

                };

                return StatusCode(500, response);
            }
        }

        [HttpPost("jobs/complete")]
        public async Task<IActionResult> CompleteCodeExecutionAsync([FromBody] CodeResponseDto podResponse, CancellationToken cancellationToken)
        {
            Console.WriteLine($"[API Controller] Got pod's response [Id:{podResponse.RequestId}], time:" + DateTime.UtcNow.ToString("o"));

            await _dispatcher.CompleteExecutionAsync(podResponse, cancellationToken);

            await _notifier.NotifyCompletedAsync(podResponse, cancellationToken);

            var createCommand = new CreateSubmissionCase(podResponse);

            await _mediator.Send(createCommand, cancellationToken);

            //TODO: here notify user and admins about submission creation

            Console.WriteLine($"[API Controller] Sent a response [Id:{podResponse.RequestId}] to client, time:" + DateTime.UtcNow.ToString("o"));

            return StatusCode(200);
        }
    }
}
