using ServerAPIApp.Domain.Exceptions.BadRequestExceptions;
using Shared.DTOs;
using Shared.Enums;
using Shared.Helpers;
using System.Diagnostics;
using System.Text.Json;

namespace Runners.Shared.Runners
{
    public class NodeJsRunner : IRunner
    {
        const string _basePath = "/tmp";
        const int _maxProcessLifetime = 25000;

        private readonly ITestWrapper _wrapper;
        private bool _isDisposed;

        static readonly ProcessStartInfo _supervisorPsi = new ProcessStartInfo
        {
            FileName = "RunnerSupervisor",
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        public NodeJsRunner(ITestWrapper wrapper)
        {
            _wrapper = wrapper;
        }

        public async Task<CodeResponseDto> ExecuteCodeAsync(ExecutionData data, CancellationToken cancellationToken = default)
        {
            var result = new CodeResponseDto()
            {
                RequestId = data.RequestId,
                UserId = data.UserId,
                UserSolution = data.UserSolution,
                Language = data.Language,
                VersionId = data.VersionId,
                Result = new ExecutionResultDto()
                {
                    RequestSentAt = data.RequestDate,
                    ResponseSentAt = DateTimeOffset.UtcNow,
                    TotalTests = data.TotalTests,
                }
            };

            var runRequest = new RunRequestDto()
            {
                ExecutorFileName = "node",
                ExecutableFileName = data.ExecutablePath,
                MaxProcessLifetime = _maxProcessLifetime
            };

            var serializedRequest = JsonSerializer.Serialize(runRequest);

            using var supervisorProc = new Process()
            {
                StartInfo = _supervisorPsi
            };

            supervisorProc.Start();

            await supervisorProc.StandardInput.WriteAsync(serializedRequest);

            supervisorProc.StandardInput.Close();

            var stdoutTask = supervisorProc.StandardOutput.ReadToEndAsync(cancellationToken);
            var stderrTask = supervisorProc.StandardError.ReadToEndAsync(cancellationToken);

            if (!supervisorProc.WaitForExit(_maxProcessLifetime))
            {
                supervisorProc.Kill();
                result.Status = RequestStatus.Failed;
                result.Result.Status = ExecutionStatus.TimedOut;
                result.Result.ExitCode = 124;
                result.Result.ConsoleOutput = "Supervisor timed out.";

                File.Delete(data.ExecutablePath);

                return result;
            }

            await Task.WhenAll(stdoutTask, stderrTask);

            var stdout = stdoutTask.Result;
            var stderr = stderrTask.Result;

            var testResult = JsonSerializer.Deserialize<RunResultDto>(stdout);

            if (testResult is null)
            {
                result.Status = RequestStatus.Failed;
                result.Result.Status = ExecutionStatus.FailedToExecute;
                result.Result.ExitCode = 1;
                result.Result.ConsoleOutput = "Failed to parse test execution result.";

                File.Delete(data.ExecutablePath);

                return result;
            }

            result = RunnerOutputParser.BuildExecutionReportOnProcOutput(result, testResult);

            File.Delete(data.ExecutablePath);

            return result;
        }

        public Task<CompilationResult> CompileCodeAsync(ProblemSolutionDto userSolution, CancellationToken cancellationToken)
        {
            ManifestDto manifest;
            try
            {
                manifest = ManifestParser.Parse(userSolution.TestManifestJson, userSolution.LanguageCode);
            }
            catch (InvalidTestTemplateException ex)
            {
                return Task.FromResult(new CompilationResult
                {
                    Success = false,
                    CompilationErrors = $"Manifest validation failed: {ex.Message}",
                    TotalTests = 0,
                    ExecutablePath = string.Empty
                });
            }
            catch (System.Text.Json.JsonException ex)
            {
                return Task.FromResult(new CompilationResult
                {
                    Success = false,
                    CompilationErrors = $"Manifest JSON parse error: {ex.Message}",
                    TotalTests = 0,
                    ExecutablePath = string.Empty
                });
            }
            catch (Exception ex)
            {
                return Task.FromResult(new CompilationResult
                {
                    Success = false,
                    CompilationErrors = $"Manifest parse error: {ex.Message}",
                    TotalTests = 0,
                    ExecutablePath = string.Empty
                });
            }

            if (manifest.SampleTests.Count == 0 && !manifest.AdvancedTests.Any(t => t.LanguageCode == userSolution.LanguageCode))
            {
                return Task.FromResult(new CompilationResult
                {
                    Success = false,
                    CompilationErrors = $"Invalid manifest: no tests for {userSolution.LanguageCode} detected",
                    TotalTests = 0,
                    ExecutablePath = string.Empty
                });
            }

            var executablePath = Path.Combine(_basePath, $"{userSolution.RequestId:N}.js");

            var fullCode = _wrapper.GenerateSource(manifest, userSolution.UserSolution, "SolutionContainer");

            File.WriteAllText(executablePath, fullCode);

            return Task.FromResult(new CompilationResult
            {
                Success = true,
                TotalTests = manifest.SampleTests.Count + manifest.AdvancedTests.Count,
                ExecutablePath = executablePath,
                CompilationErrors = null
            });
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