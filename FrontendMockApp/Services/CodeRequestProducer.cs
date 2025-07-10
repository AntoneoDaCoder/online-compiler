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
        private const int _maxTimeoutInMilliseconds = 2000;

        private Uri _requestUri;

        private HttpClient _httpClient;
        private IRequestObserver _requestObserver;
        private CancellationTokenSource? _cts;
        private Task? _postingTask;
        private bool _isPosting = false;
        private bool _isDisposed = false;
        private string _callbackUrl;

        private Dictionary<string, string> _testExamples = new Dictionary<string, string>();

        public CodeRequestProducer(Dictionary<string, string> examples, string callbackUrl, IRequestObserver requestObserver)
        {
            _httpClient = new HttpClient();
            _testExamples = examples;
            _requestObserver = requestObserver;

            var baseUri = new Uri(_serverTestUrl, UriKind.Absolute);
            var serverTestEndpoint = new Uri(_serverTestEndpoint, UriKind.Relative);
            _requestUri = new Uri(baseUri, serverTestEndpoint);
            _callbackUrl = callbackUrl;
        }

        public void Start()
        {
            Console.WriteLine("[Producer] is starting...");

            if (_isPosting)
            {
                return;
            }

            _isPosting = true;

            _cts = new CancellationTokenSource();
            _postingTask = PostAsync(_cts.Token);

            Console.WriteLine("[Producer] is ready to send requests.");
        }

        public async Task StopAsync()
        {
            Console.WriteLine("[Producer] is stopping...");

            if (!_isPosting)
            {
                return;
            }

            _isPosting = false;

            _cts?.Cancel();

            if (_postingTask != null)
                await _postingTask;

            _cts?.Dispose();
            _cts = null;

            Console.WriteLine("[Producer] stopped.");
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
                _cts?.Cancel();

                if (_postingTask != null)
                {
                    try
                    {
                        _postingTask.GetAwaiter().GetResult();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Producer] Error while disposing posting task: {ex}");
                    }
                }

                _cts?.Dispose();
                _cts = null;
                _postingTask = null;
                _isPosting = false;
                _httpClient.Dispose();
            }

            _isDisposed = true;
        }

        private async Task PostAsync(CancellationToken cancellationToken)
        {
            //while (!cancellationToken.IsCancellationRequested)
            //{


            //}
            if (!cancellationToken.IsCancellationRequested)
            {
                // for (int i = 0; i < 5; i++)
                //foreach (var example in _testExamples)
                //{
                //    if (cancellationToken.IsCancellationRequested)
                //        break;

                //    var requestDto = new CodeRequestDto()
                //    {
                //        Code = example.Value,
                //        MaxAllowedTimeInMilliseconds = _maxTimeoutInMilliseconds,
                //        CallbackUrl = _callbackUrl,
                //        RequestSentAt = DateTime.UtcNow
                //    };

                //    await PostSingleExecutionRequestAsync(requestDto, cancellationToken);
                //}

                var example = _testExamples.First();

                var requestDto = new CodeRequestDto()
                {
                    Code = example.Value,
                    MaxAllowedTimeInMilliseconds = _maxTimeoutInMilliseconds,
                    CallbackUrl = _callbackUrl,
                    RequestSentAt = DateTime.UtcNow
                };

                await PostSingleExecutionRequestAsync(requestDto, cancellationToken);
            }
        }
    }
}
