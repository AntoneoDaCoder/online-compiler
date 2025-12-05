using Microsoft.AspNetCore.Mvc;
using Shared.DTOs;
using Shared.Enums;
using ServerAPIApp.Contracts.Abstractions;
using Microsoft.AspNetCore.SignalR;
using ServerAPIApp.Contracts.DTOs;

namespace ServerAPIApp.Controllers
{
    [ApiController]
    [Route("api")]
    public class ServerController : ControllerBase
    {
        private ICodeDispatcher _dispatcher;
        private ISubmissionNotifier _notifier;

        public ServerController(ICodeDispatcher dispatcher, ISubmissionNotifier notifier)
        {
            _dispatcher = dispatcher;
            _notifier = notifier;
        }

        [HttpPost("jobs/start")]
        public async Task<IActionResult> ScheduleCodeExecutionAsync([FromBody] CodeRequestDto dto, CancellationToken cancellationToken)
        {
            try
            {
                await _dispatcher.ScheduleForExecutionAsync(dto, cancellationToken);

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

                return Accepted(response);
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

            Console.WriteLine($"[API Controller] Sent a response [Id:{podResponse.RequestId}] to client, time:" + DateTime.UtcNow.ToString("o"));

            return Ok();
        }
    }
}
