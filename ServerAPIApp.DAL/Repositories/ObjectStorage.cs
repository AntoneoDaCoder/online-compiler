using ServerAPIApp.Contracts.Abstractions;
using Minio;

namespace ServerAPIApp.DAL.Repositories
{
    public class ObjectStorage : IObjectStorage
    {
        private IMinioClient _client;
        public ObjectStorage(string endpoint, string accessKey, string secretKey, bool useSsl = true)
        {
            _client = new MinioClient()
                .WithEndpoint(endpoint)
                .WithCredentials(accessKey, secretKey)
                .WithSSL(useSsl)
                .Build();
        }

        public async Task<bool> UploadStringAsync
            (string bucket, string key, string content, string contentType = "application/json", CancellationToken cancellationToken = default)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(content);

            using var ms = new MemoryStream(bytes);

            var result = await _client.PutObjectAsync(
                new Minio.DataModel.Args.PutObjectArgs()
                .WithBucket(bucket)
                .WithObject(key)
                .WithStreamData(ms)
                .WithObjectSize(ms.Length)
                .WithContentType(contentType),
                cancellationToken
                );

            return (int)result.ResponseStatusCode > 199 && (int)result.ResponseStatusCode < 300;
        }

        public async Task<string?> GetStringAsync(string bucket, string key, CancellationToken cancellationToken = default)
        {
            using var ms = new MemoryStream();

            await _client.GetObjectAsync(
                new Minio.DataModel.Args.GetObjectArgs()
                .WithBucket(bucket)
                .WithObject(key)
                .WithCallbackStream((stream) => stream.CopyTo(ms)),
                cancellationToken
                );

            ms.Seek(0, SeekOrigin.Begin);

            using var reader = new StreamReader(ms);
            return await reader.ReadToEndAsync(cancellationToken);
        }

        public async Task<bool> DeleteObjectAsync(string bucket, string key, CancellationToken cancellationToken = default)
        {
            await _client.RemoveObjectAsync
                      (
                      new Minio.DataModel.Args.RemoveObjectArgs()
                      .WithBucket(bucket)
                      .WithObject(key),
                      cancellationToken
                      );

            return true;
        }
    }
}
