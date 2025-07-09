using FrontendMockApp.Abstractions;
using Shared.DTOs;
using System.Text.Json;
using System.Collections.Concurrent;

namespace FrontendMockApp.Services
{
    public class RequestManager : IRequestManager
    {
        private static JsonSerializerOptions _options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        private ConcurrentDictionary<Guid, CodeResponseDto> _pendingRequests = new ConcurrentDictionary<Guid, CodeResponseDto>();

        public void ConsumeMessage(string message)
        {
            var serverResponse = JsonSerializer.Deserialize<CodeResponseDto>(message, _options);

            _pendingRequests.AddOrUpdate(
                serverResponse!.RequestId,
                serverResponse,
                (key, existing) =>
                {
                    existing.Status = serverResponse.Status;

                    existing.Result = serverResponse.Result;

                    return existing;
                });

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
