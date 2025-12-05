using Shared.DTOs;

namespace ServerAPIApp.Contracts.Abstractions
{
    public interface ISubmissionNotifier
    {
        Task NotifyFailedAsync
            (Guid submissionId,
            string reason,
            CancellationToken cancellationToken = default);
        Task NotifyCompletedAsync
            (CodeResponseDto response,
            CancellationToken cancellationToken = default);
        //TODO: create submissioncreated notification method
    }
}
