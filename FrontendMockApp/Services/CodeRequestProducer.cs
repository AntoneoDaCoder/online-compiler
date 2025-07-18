using Shared.DTOs;
using System.Text.Json;
using System.Text;
using FrontendMockApp.Abstractions;

namespace FrontendMockApp.Services
{
    public class CodeRequestProducer : IDisposable
    {
        private const string _serverTestUrl = "http://localhost:12345/api/";
        private const string _serverTestEndpoint = "jobs/start";

        private Uri _requestUri;
        private HttpClient _httpClient;
        private IRequestObserver _requestObserver;
        private bool _isDisposed = false;

        public CodeRequestProducer(IRequestObserver requestObserver)
        {
            _httpClient = new HttpClient();
            _requestObserver = requestObserver;

            var baseUri = new Uri(_serverTestUrl, UriKind.Absolute);
            var serverTestEndpoint = new Uri(_serverTestEndpoint, UriKind.Relative);
            _requestUri = new Uri(baseUri, serverTestEndpoint);
        }

        public async Task PostSingleExecutionRequestAsync(CodeRequestDto codeRequest, CancellationToken cancellationToken)
        {
            ArgumentException.ThrowIfNullOrEmpty(codeRequest.CallbackUrl, nameof(codeRequest.CallbackUrl));

            var serializedDto = JsonSerializer.Serialize(codeRequest);
            var requestContent = new StringContent(serializedDto, Encoding.UTF8, "application/json");

            try
            {
                var serverInitResponse = await _httpClient.PostAsync(_requestUri, requestContent, cancellationToken);

                var msgContent = await serverInitResponse.Content.ReadAsStringAsync(cancellationToken);

                _requestObserver.NotifySubscribers(msgContent);
            }
            catch (TaskCanceledException)
            {

            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Producer] Error while sending request: {ex}");
            }
        }



        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool isDisposing)
        {
            if (_isDisposed)
            {
                return;
            }

            if (isDisposing)
            {
                _httpClient.Dispose();
            }

            _isDisposed = true;
        }
    }
}
