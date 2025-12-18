using Shared.DTOs;

namespace ServerAPIApp.Contracts.Abstractions
{
    public interface ISubmissionNotifier
    {
        Task NotifyAsync
           (string userId,
            object? data,
            CancellationToken cancellationToken = default);
    }
}
