using Microsoft.AspNetCore.SignalR;
using ServerAPIApp.Contracts.Abstractions;
using Shared.DTOs;

namespace ServerAPIApp.Notifiers
{
    public class SubmissionNotifier : ISubmissionNotifier
    {
        private INotificationService _service;

        public SubmissionNotifier(INotificationService service)
        {
            _service = service;
        }

        public async Task NotifyAsync
            (string userId,
             object? data,
             CancellationToken cancellationToken = default)
        {
            await _service.NotifyUserAsync
                (
                userId,
                "ExecutionCompleted",
                data,
                cancellationToken);
        }
    }
}
