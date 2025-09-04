using Shared.DTOs;
using System.Diagnostics;
using System.Text.Json;

namespace Runners.Shared.Runners
{
    public class KotlinCompositeRunner : IRunner
    {
        private static JsonSerializerOptions _options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        private static ProcessStartInfo _pInfo = new ProcessStartInfo()
        {
            FileName = "java",
            Arguments = "-jar runner.jar --once",
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        private string _serializedDto = string.Empty;
        private bool _isDisposed;

        public async Task<CodeResponseDto> ExecuteCodeAsync(Guid requestId, DateTime requestDate, CancellationToken cancellationToken)
        {
            using var proc = new Process()
            {
                StartInfo = _pInfo,
            };

            proc.Start();
            await proc.StandardInput.WriteLineAsync(_serializedDto);
            proc.StandardInput.Close();

            string output = await proc.StandardOutput.ReadToEndAsync(cancellationToken);
            string errors = await proc.StandardError.ReadToEndAsync(cancellationToken);

            await proc.WaitForExitAsync(cancellationToken);

            var response = JsonSerializer.Deserialize<CodeResponseDto>(output, _options);

            return response!;
        }

        public Task<(bool Success, string CompilationErrors)> CompileCodeAsync(string fullCode, CancellationToken cancellationToken)
        {
            _serializedDto = fullCode;

            if (string.IsNullOrEmpty(_serializedDto))
                return Task.FromResult((false, "Failed to compile code: no code provided"));

            return Task.FromResult((true, string.Empty));
        }

        public string WrapCode(ProblemSolutionDto problemSolutionDto)
        {
            return JsonSerializer.Serialize(problemSolutionDto, _options);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed)
            {
                return;
            }
            if (disposing)
            {
            }
            _isDisposed = true;
        }
    }
}
