using Microsoft.AspNetCore.SignalR;
using ServerAPIApp.Contracts.Abstractions;
using ServerAPIApp.Hubs;

namespace ServerAPIApp.Notifiers
{
    public class NotificationService : INotificationService
    {
        private readonly IHubContext<UserHub> _hub;

        public NotificationService(IHubContext<UserHub> hub)
        {
            _hub = hub;
        }

        public Task NotifyUserAsync(string userId, string clientMethod, object? data, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentException(nameof(userId));

            if (string.IsNullOrWhiteSpace(clientMethod)) throw new ArgumentException(nameof(clientMethod));

            return _hub.Clients.User(userId).SendAsync(clientMethod, data, cancellationToken);
        }

        public Task NotifyGroupAsync(string groupName, string clientMethod, object? data, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(groupName)) throw new ArgumentException(nameof(groupName));

            if (string.IsNullOrWhiteSpace(clientMethod)) throw new ArgumentException(nameof(clientMethod));

            return _hub.Clients.Group(groupName).SendAsync(clientMethod, data, cancellationToken);
        }
    }
}
