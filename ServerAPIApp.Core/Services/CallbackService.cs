using Shared.DTOs;
using System.Net.Http.Json;

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

            try
            {
                await _client.PostAsJsonAsync(callbackUrl, response, cancellationToken);
            }
            catch
            {

            }
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
