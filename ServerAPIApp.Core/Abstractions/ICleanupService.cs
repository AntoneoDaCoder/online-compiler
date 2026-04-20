namespace ServerAPIApp.Core.Abstractions
{
    public interface ICleanupService
    {
        Task DeleteProblemRelatedMetadataAsync(Guid requestId, string jobId, CancellationToken cancellationToken = default);
    }
}
