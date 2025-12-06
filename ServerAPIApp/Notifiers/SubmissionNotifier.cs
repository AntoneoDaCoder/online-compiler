using Microsoft.AspNetCore.SignalR;
using ServerAPIApp.Contracts.Abstractions;
using ServerAPIApp.Hubs;
using Shared.DTOs;

namespace ServerAPIApp.Notifiers
{
    public class SubmissionNotifier : ISubmissionNotifier
    {
        private IHubContext<ResultHub> _hub;

        public SubmissionNotifier(IHubContext<ResultHub> hub)
        {
            _hub = hub;
        }

        public async Task NotifyFailedAsync
            (Guid requestId,
             string reason,
             CancellationToken cancellationToken = default)
        {
            await _hub.Clients
                .Group(requestId.ToString())
                .SendAsync("RequestFailed",
                new
                {
                    RequestId = requestId,
                    Reason = reason
                },
                cancellationToken);
        }

        public async Task NotifyCompletedAsync
            (CodeResponseDto response,
            CancellationToken cancellationToken = default)
        {
            await _hub.Clients
                .Group(response.RequestId.ToString())
                .SendAsync("ExecutionCompleted", response, cancellationToken);
        }
    }
}
