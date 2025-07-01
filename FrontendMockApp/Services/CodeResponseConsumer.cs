using FrontendMockApp.Abstractions;
using System.Net;

namespace FrontendMockApp.Services
{
    public class CodeResponseConsumer : IDisposable
    {
        private HttpListener _listener;
        private Task? _listeningTask;
        private CancellationTokenSource? _cts;
        private IRequestObserver _requestObserver;

        private bool _disposed;
        private volatile bool _isListening;

        public CodeResponseConsumer(string urlPrefix, IRequestObserver requestObserver)
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add(urlPrefix);
            _requestObserver = requestObserver;
        }

        public void Start()
        {
            Console.WriteLine("[Consumer] is starting...");

            if (_isListening)
            {
                return;
            }

            _isListening = true;

            _cts = new CancellationTokenSource();

            _listener.Start();
            _listeningTask = Task.Run(() => ListenAsync(_cts.Token), _cts.Token);

            Console.WriteLine("[Consumer] is ready to accept incoming requests.");
        }

        public async Task StopAsync()
        {
            Console.WriteLine("[Consumer] is stopping...");

            if (!_isListening)
            {
                return;
            }
            _isListening = false;

            _cts?.Cancel();

            _listener.Stop();

            if (_listeningTask is not null)
            {
                await _listeningTask;
            }

            _cts?.Dispose();
            _listeningTask = null;
            _cts = null;
            _isListening = false;

            Console.WriteLine("[Consumer] stopped.");
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
                _cts?.Cancel();

                if (_listeningTask is not null)
                {
                    try
                    {
                        _listeningTask.GetAwaiter().GetResult();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Consumer] Error while disposing listening task: {ex}");
                    }
                }

                _cts?.Dispose();
                _listeningTask = null;
                _cts = null;
                _listener.Close();
                _isListening = false;
            }
            _disposed = true;
        }

        private async Task ListenAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var context = await _listener.GetContextAsync();

                    using (var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding))
                    {
                        try
                        {
                            var serverResponseString = await reader.ReadToEndAsync(cancellationToken);

                            _requestObserver.NotifySubscribers(serverResponseString);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error processing callback: {ex}");
                        }
                        finally
                        {
                            context.Response.Close();
                        }
                    }

                }
            }
            catch (HttpListenerException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Consumer] Error in listening loop: {ex}");
            }
        }
    }
}
