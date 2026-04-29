using ServerAPIApp.Domain.Exceptions.BadRequestExceptions;
using Shared.DTOs;
using Shared.Enums;
using Shared.Helpers;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace Runners.Shared.Runners
{
    public partial class NodeJsRunner : IRunner
    {
        const string _basePath = "/tmp";
        const int _maxProcessLifetime = 25000;

        private readonly ITestWrapper _wrapper;
        private bool _isDisposed;

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

            var pInfo = new ProcessStartInfo
            {
                FileName = "node",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            pInfo.ArgumentList.Add(data.ExecutablePath);

            using var proc = new Process
            {
                StartInfo = pInfo,
            };

            try
            {
                proc.Start();

                var stdoutTask = proc.StandardOutput.ReadToEndAsync(cancellationToken);
                var stderrTask = proc.StandardError.ReadToEndAsync(cancellationToken);

                if (!proc.WaitForExit(_maxProcessLifetime))
                {
                    try
                    {
                        proc.Kill(entireProcessTree: true);
                    }
                    catch
                    {
                    }

                    result.Status = RequestStatus.Failed;
                    result.Result.Status = ExecutionStatus.TimedOut;
                    result.Result.ExitCode = 124;
                    result.Result.ConsoleOutput = "Execution timed out.";

                    return result;
                }

                result.Result.ExitCode = proc.ExitCode;

                await Task.WhenAll(stdoutTask, stderrTask);

                var stdOut = stdoutTask.Result ?? string.Empty;
                var stdErr = stderrTask.Result ?? string.Empty;

                var passedMatch = PassedTestsRegex().Match(stdOut);
                result.Result.PassedTests =
                    passedMatch.Success && int.TryParse(passedMatch.Groups[1].Value, out var p)
                        ? p
                        : 0;

                if (proc.ExitCode == 0)
                {
                    result.Status = RequestStatus.Succeeded;
                    result.Result.Status = ExecutionStatus.Succeeded;
                    result.Result.ConsoleOutput = stdOut;
                }
                else
                {
                    result.Status = RequestStatus.Failed;

                    var failedMatches = FailedTestsRegex().Matches(stdOut);
                    var failedList = new List<(string TestName, string Reason)>();

                    foreach (Match m in failedMatches)
                    {
                        if (!m.Success)
                        {
                            continue;
                        }

                        var name = m.Groups[1].Value.Trim();
                        var reason = m.Groups[2].Value.Trim()
                            .Replace("\r", "")
                            .Replace("\n", " ");

                        failedList.Add((name, reason));
                    }

                    if (failedList.Count > 0)
                    {
                        bool anyTimeout = failedList.Any(f =>
                            f.Reason.Contains("timed out", StringComparison.OrdinalIgnoreCase) ||
                            f.Reason.Contains("timeout", StringComparison.OrdinalIgnoreCase) ||
                            f.Reason.Contains("Test execution timed out", StringComparison.OrdinalIgnoreCase));

                        result.Result.Status = anyTimeout ? ExecutionStatus.TimedOut : ExecutionStatus.FailedToExecute;

                        var sb = new StringBuilder();
                        sb.AppendLine("Failed tests:");
                        foreach (var f in failedList)
                        {
                            sb.AppendLine($"{f.TestName} - {f.Reason}");
                        }

                        sb.AppendLine();
                        sb.AppendLine("--- STDOUT ---");
                        sb.AppendLine(stdOut.Trim());
                        sb.AppendLine();
                        sb.AppendLine("--- STDERR ---");
                        sb.AppendLine(stdErr.Trim());

                        result.Result.ConsoleOutput = sb.ToString().Trim();
                    }
                    else
                    {
                        var combined = $"{stdOut}\n{stdErr}";

                        if (combined.Contains("timed out", StringComparison.OrdinalIgnoreCase) ||
                            combined.Contains("timeout", StringComparison.OrdinalIgnoreCase))
                        {
                            result.Result.Status = ExecutionStatus.TimedOut;
                        }
                        else if (combined.Contains("assert", StringComparison.OrdinalIgnoreCase) ||
                                 combined.Contains("failed", StringComparison.OrdinalIgnoreCase))
                        {
                            result.Result.Status = ExecutionStatus.FailedToExecute;
                        }
                        else
                        {
                            result.Result.Status = ExecutionStatus.RuntimeError;
                        }

                        var sb = new StringBuilder();
                        sb.AppendLine("--- STDOUT ---");
                        sb.AppendLine(stdOut.Trim());
                        sb.AppendLine();
                        sb.AppendLine("--- STDERR ---");
                        sb.AppendLine(stdErr.Trim());

                        result.Result.ConsoleOutput = sb.ToString().Trim();
                    }
                }

                return result;
            }
            finally
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(data.ExecutablePath) && File.Exists(data.ExecutablePath))
                    {
                        File.Delete(data.ExecutablePath);
                    }
                }
                catch
                {
                }
            }
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
                CompilationErrors=null
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

        [GeneratedRegex(@"PassedTests\s*[:=]\s*(\d+)", RegexOptions.IgnoreCase)]
        private static partial Regex PassedTestsRegex();

        [GeneratedRegex(@"FailedTest:([^\:]+):(.*)", RegexOptions.IgnoreCase)]
        private static partial Regex FailedTestsRegex();
    }
}