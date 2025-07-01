using Microsoft.AspNetCore.Mvc;
using ServerAPIApp.Core.DTOs;
using ServerAPIApp.Core.Enums;
using ServerAPIApp.Core.Services;
using System.Text.Json;

namespace ServerAPIApp.Controllers
{
    [ApiController]
    public class ServerController : ControllerBase
    {
        private CodeDispatcher _dispatcher;
        public ServerController(CodeDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        [HttpPost("execute")]
        public async Task<IActionResult> ExecuteCode([FromBody] CodeRequestDto dto, CancellationToken cancellationToken)
        {
            try
            {
                await _dispatcher.ScheduleForExecutionAsync(dto, cancellationToken);

                var response = new CodeResponseDto()
                {
                    RequestId = dto.RequestId,
                    Status = RequestStatus.Acknowledged,
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
    }
}
