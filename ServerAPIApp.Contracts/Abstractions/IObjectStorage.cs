namespace ServerAPIApp.Contracts.Abstractions
{
    public interface IObjectStorage
    {
        Task<bool> UploadStringAsync
            (string bucket,
            string key,
            string content,
            string contentType = "application/json",
            CancellationToken cancellationToken = default);
        Task<string?> GetStringAsync
            (string bucket,
            string key,
            CancellationToken cancellationToken = default);
        Task<bool> DeleteObjectAsync
            (string bucket,
            string key,
            CancellationToken cancellationToken = default);
    }
}
