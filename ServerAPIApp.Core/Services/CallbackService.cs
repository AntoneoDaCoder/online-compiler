using ServerAPIApp.Core.DTOs;
using System.Text.Json;
using System.Text;

namespace ServerAPIApp.Core.Services
{
    public class CallbackService : IDisposable
    {
        private HttpClient _client;
        private bool _disposed;

        public CallbackService()
        {
            _client = new HttpClient();
        }

        public async Task NotifyClientAsync(CodeResponseDto response, string callbackUrl, CancellationToken cancellationToken)
        {
            response.Result.ResponseSentAt = DateTime.UtcNow;

            var serializedDto = JsonSerializer.Serialize(response);
            var requestContent = new StringContent(serializedDto, Encoding.UTF8, "application/json");

            await _client.PostAsync(callbackUrl, requestContent, cancellationToken);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }
            if (disposing)
            {
                _client.Dispose();
            }
            _disposed = true;
        }
    }
}
