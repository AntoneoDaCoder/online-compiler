namespace ServerAPIApp.Core.Abstractions
{
    public interface ICleanupService
    {
        Task DeleteProblemRelatedMetadataAsync(Guid requestId, string jobId, CancellationToken cancellationToken = default);

        Task DeleteUserRelatedMetadataAsync(string userId, string jobId, CancellationToken cancellationToken = default);

        void DeleteCompletedJob(string jobId);
    }
}
