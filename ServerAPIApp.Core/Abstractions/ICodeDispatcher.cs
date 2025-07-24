using Microsoft.Extensions.Hosting;
using Shared.DTOs;

namespace ServerAPIApp.Core.Abstractions
{
    public interface ICodeDispatcher : IHostedService, IDisposable
    {
        Task ScheduleForExecutionAsync(CodeRequestDto request, CancellationToken cancellationToken);
        Task CompleteExecutionAsync(CodeResponseDto response, CancellationToken cancellationToken);
    }
}
