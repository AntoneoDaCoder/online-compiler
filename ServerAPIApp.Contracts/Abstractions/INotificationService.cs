namespace ServerAPIApp.Contracts.Abstractions
{
    public interface INotificationService
    {
        Task NotifyUserAsync(string userId, string clientMethod, object? data, CancellationToken cancellationToken = default);
        Task NotifyGroupAsync(string groupName, string clientMethod, object? data, CancellationToken cancellationToken = default);
    }
}
