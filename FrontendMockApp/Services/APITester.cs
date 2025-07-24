using FrontendMockApp.Abstractions;
using FrontendMockApp.Models;
using Shared.DTOs;
using Shared.Enums;
using System.Collections.Concurrent;
using System.Text.Json;

namespace FrontendMockApp.Services
{
    public class APITester : IDisposable, IRequestManager
    {
        private static JsonSerializerOptions _options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        private TestingStatistics _stats;
        private CodeRequestProducer _producer;
        private CodeResponseConsumer _consumer;
        private IRequestObserver _requestObserver;

        private readonly long _maxTimeoutInMilliseconds = 2000;
        private CancellationTokenSource? _cts;
        private Task? _postingTask;
        private bool _isPosting = false;
        private Dictionary<string, string> _testExamples = new Dictionary<string, string>();
        private string _callbackUrl;
        private bool _disposed;

        private ConcurrentDictionary<Guid, CodeResponseDto> _pendingRequests = new ConcurrentDictionary<Guid, CodeResponseDto>();

        public APITester(string callbackUrl, string urlPrefix, Dictionary<string, string> examples)
        {
            _callbackUrl = callbackUrl;
            _testExamples = examples;
            _requestObserver = new RequestObserver();
            _requestObserver.Subscribe(this);

            _producer = new CodeRequestProducer(_requestObserver);
            _consumer = new CodeResponseConsumer(urlPrefix, _requestObserver);
        }

        public void StartSession(string sessionDesc)
        {
            _stats = new TestingStatistics()
            {
                CaseDescription = sessionDesc
            };

            if (_isPosting)
            {
                return;
            }

            _isPosting = true;

            _cts = new CancellationTokenSource();
            _postingTask = PostAsync(_cts.Token);

            _consumer.Start();
        }

        public async Task<TestingStatistics> EndSession()
        {
            await _consumer.StopAsync();

            _stats.SessionEndedAt = DateTime.UtcNow;

            if (!_isPosting)
            {
                return _stats;
            }

            _isPosting = false;

            _cts?.Cancel();

            if (_postingTask != null)
                await _postingTask;

            _cts?.Dispose();
            _cts = null;

            return _stats;
        }

        public virtual void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void ConsumeMessage(string message)
        {
            var serverResponse = JsonSerializer.Deserialize<CodeResponseDto>(message, _options);

            if (!_pendingRequests.TryGetValue(serverResponse!.RequestId, out var existing))
            {
                _pendingRequests[serverResponse.RequestId] = serverResponse;

                _stats.ResponsesReceived++;
            }
            else
            {
                var temp = _stats.ResponsesWithStatusCode;
                if (serverResponse.Status != RequestStatus.Acknowledged)
                    _stats.ResponsesWithStatusCode++;

                existing.Status = serverResponse.Status;

                existing.Result = serverResponse.Result;

                _stats.AverageLatency = (_stats.AverageLatency * temp + existing.Result.LatencyInSeconds) / _stats.ResponsesWithStatusCode;
            }

            RenderFrame();
        }

        public void RenderFrame()
        {
            Console.Clear();
            foreach (var response in _pendingRequests)
            {
                string requestId = response.Key.ToString();
                string reqStatus = response.Value.Status.ToString();
                string execStatus = response.Value.Result?.Status.ToString() ?? "null";
                string exitCode = response.Value.Result?.ExitCode.ToString() ?? "null";
                string output = response.Value.Result?.ConsoleOutput != null
                    ? Truncate(response.Value.Result.ConsoleOutput.Replace("\n", " ").Replace("\r", ""), 40)
                    : "<no output>";
                string latencyInSeconds = response.Value.Result?.LatencyInSeconds.ToString() ?? "null";

                Console.WriteLine(
                    $"| ReqId: {Shorten(requestId)} | ReqSt: {reqStatus,-10} | ExecSt: {execStatus,-10} | Exit: {exitCode,3} |" +
                    $" Out: {output,-40} | Latency: {latencyInSeconds} sec |");

            }
        }

        protected void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }
            if (disposing)
            {
                _consumer.Dispose();

                _cts?.Cancel();

                if (_postingTask != null)
                {
                    try
                    {
                        _postingTask.GetAwaiter().GetResult();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Tester] Error while disposing posting task: {ex}");
                    }
                }

                _cts?.Dispose();
                _cts = null;
                _postingTask = null;
                _isPosting = false;

                _producer.Dispose();
            }
            _disposed = true;
        }

        private async Task PostAsync(CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                // for (int i = 0; i < 30; i++)
                //   for (int i = 0; i < 10; i++)
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
                //        RequestSentAt = DateTime.UtcNow,
                //        ProblemName = example.Key,
                //    };

                //    _stats.RequestsSent++;

                //    await _producer.PostSingleExecutionRequestAsync(requestDto, cancellationToken);
                //}

                var heavyOk = _testExamples["HeavyCorrectExample.cs"];
                var heavyBad = _testExamples["TimeoutExample.cs"];
                var lightOk = _testExamples["LightCorrectExample.cs"];
                var lightBad = _testExamples["CompileErrorExample.cs"];
                var lightCheating = _testExamples["CheatingExample.cs"];

                var requestDto = new CodeRequestDto()
                {
                    Code = heavyOk,
                    MaxAllowedTimeInMilliseconds = _maxTimeoutInMilliseconds,
                    CallbackUrl = _callbackUrl,
                    RequestSentAt = DateTime.UtcNow,
                    ProblemName = "HeavyCorrectExample.cs",
                };

                _stats.RequestsSent++;

                await _producer.PostSingleExecutionRequestAsync(requestDto, cancellationToken);

                requestDto = new CodeRequestDto()
                {
                    Code = heavyBad,
                    MaxAllowedTimeInMilliseconds = _maxTimeoutInMilliseconds,
                    CallbackUrl = _callbackUrl,
                    RequestSentAt = DateTime.UtcNow,
                    ProblemName = "TimeoutExample.cs",
                };

                _stats.RequestsSent++;

                await _producer.PostSingleExecutionRequestAsync(requestDto, cancellationToken);

                requestDto = new CodeRequestDto()
                {
                    Code = lightOk,
                    MaxAllowedTimeInMilliseconds = _maxTimeoutInMilliseconds,
                    CallbackUrl = _callbackUrl,
                    RequestSentAt = DateTime.UtcNow,
                    ProblemName = "LightCorrectExample.cs",
                };

                _stats.RequestsSent++;

                await _producer.PostSingleExecutionRequestAsync(requestDto, cancellationToken);

                requestDto = new CodeRequestDto()
                {
                    Code = lightBad,
                    MaxAllowedTimeInMilliseconds = _maxTimeoutInMilliseconds,
                    CallbackUrl = _callbackUrl,
                    RequestSentAt = DateTime.UtcNow,
                    ProblemName = "CompileErrorExample.cs",
                };

                _stats.RequestsSent++;

                await _producer.PostSingleExecutionRequestAsync(requestDto, cancellationToken);

                requestDto = new CodeRequestDto()
                {
                    Code = lightCheating,
                    MaxAllowedTimeInMilliseconds = _maxTimeoutInMilliseconds,
                    CallbackUrl = _callbackUrl,
                    RequestSentAt = DateTime.UtcNow,
                    ProblemName = "CheatingExample.cs",
                };

                _stats.RequestsSent++;

                await _producer.PostSingleExecutionRequestAsync(requestDto, cancellationToken);

                //var example = _testExamples.First();

                //var requestDto = new CodeRequestDto()
                //{
                //    Code = example.Value,
                //    MaxAllowedTimeInMilliseconds = _maxTimeoutInMilliseconds,
                //    CallbackUrl = _callbackUrl,
                //    RequestSentAt = DateTime.UtcNow
                //};

                //await PostSingleExecutionRequestAsync(requestDto, cancellationToken);
            }
        }

        private static string Truncate(string value, int maxLength)
        {
            return value.Length <= maxLength ? value : value.Substring(0, maxLength - 3) + "...";
        }

        private static string Shorten(string guid, int length = 8)
        {
            return guid.Length > length ? guid.Substring(0, length) : guid;
        }
    }
}
