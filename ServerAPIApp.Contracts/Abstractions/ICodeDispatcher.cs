using Microsoft.Extensions.Hosting;
using Shared.DTOs;
using ServerAPIApp.Contracts.DTOs;

namespace ServerAPIApp.Contracts.Abstractions
{
    public interface ICodeDispatcher : IHostedService, IDisposable
    {
        Task ScheduleForExecutionAsync(Guid userId, CodeRequestDto request, CancellationToken cancellationToken);
        Task CompleteExecutionAsync(CodeResponseDto response, CancellationToken cancellationToken);
    }
}
