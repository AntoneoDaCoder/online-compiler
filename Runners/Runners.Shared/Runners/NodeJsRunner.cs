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
        static ProcessStartInfo _pInfo = new ProcessStartInfo()
        {
            FileName = "node",
            Arguments = "/tmp/UserProgram.js",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        const string _tmpJsFilePath = "/tmp/UserProgram.js";

        const int _maxProcessLifetime = 25000;

        private ITestWrapper _wrapper;

        bool _isDisposed;

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
                    TotalTests = data.TotalTests,
                }
            };

            using var proc = new Process
            {
                StartInfo = _pInfo,
            };

            proc.Start();
            if (!proc.WaitForExit(_maxProcessLifetime))
            {
                proc.Kill();
                result.Status = RequestStatus.Failed;
                result.Result.Status = ExecutionStatus.TimedOut;
                result.Result.ExitCode = 124;
                result.Result.ConsoleOutput = "Execution timed out.";

                File.Delete(_tmpJsFilePath);

                return result;
            }

            result.Result.ExitCode = proc.ExitCode;

            var stdOut = await proc.StandardOutput.ReadToEndAsync(cancellationToken) ?? string.Empty;
            var stdErr = await proc.StandardError.ReadToEndAsync(cancellationToken) ?? string.Empty;

            int passedCount = 0;
            var passedMatch = PassedTestsRegex().Match(stdOut ?? string.Empty);

            if (passedMatch.Success && int.TryParse(passedMatch.Groups[1].Value, out var p))
            {
                passedCount = p;
            }
            result.Result.PassedTests = passedCount;

            if (proc.ExitCode == 0)
            {
                result.Status = RequestStatus.Succeeded;
                result.Result.Status = ExecutionStatus.Succeeded;
            }
            else
            {
                result.Status = RequestStatus.Failed;
                result.Result.Status = ExecutionStatus.RuntimeError;

                // 1) find FailedTest entries: pattern "FailedTest:<TestName>:<Reason>"
                var failedMatches = FailedTestsRegex().Matches(stdOut ?? string.Empty);
                var failedList = new List<(string TestName, string Reason)>();
                foreach (Match m in failedMatches)
                {
                    if (m.Success)
                    {
                        var name = m.Groups[1].Value.Trim();
                        var reason = m.Groups[2].Value.Trim();
                        reason = reason.Replace("\r", "").Replace("\n", " ").Trim();
                        failedList.Add((name, reason));
                    }
                }

                int failedCount = failedList.Count;

                if (failedCount > 0)
                {
                    // There were test failures — decide if any of them indicate a timeout
                    bool anyTimeout = failedList.Any(f => f.Reason.Contains("timed out", StringComparison.OrdinalIgnoreCase)
                                                         || f.Reason.Contains("timeout", StringComparison.OrdinalIgnoreCase)
                                                         || f.Reason.Contains("Test execution timed out", StringComparison.OrdinalIgnoreCase));

                    result.Result.Status = anyTimeout ? ExecutionStatus.TimedOut : ExecutionStatus.FailedToExecute;

                    var sb = new StringBuilder();
                    sb.AppendLine("Failed tests:");
                    foreach (var f in failedList)
                        sb.AppendLine($"{f.TestName} - {f.Reason}");

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
                    // No explicit FailedTest markers found — try to infer from stdout/stderr contents
                    var combined = (stdOut + "\n" + stdErr) ?? string.Empty;
                    if (combined.Contains("timed out", StringComparison.OrdinalIgnoreCase)
                        || combined.Contains("timeout", StringComparison.OrdinalIgnoreCase))
                    {
                        result.Result.Status = ExecutionStatus.TimedOut;
                    }
                    else if (combined.Contains("assert", StringComparison.OrdinalIgnoreCase)
                             || combined.Contains("failed", StringComparison.OrdinalIgnoreCase))
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

            File.Delete(_tmpJsFilePath);

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
                return Task.FromResult
                    (
                    new CompilationResult()
                    {
                        Success = false,
                        CompilationErrors = $"Manifest validation failed: {ex.Message}"
                    }
                    );
            }
            catch (System.Text.Json.JsonException ex)
            {
                return Task.FromResult
                  (
                  new CompilationResult()
                  {
                      Success = false,
                      CompilationErrors = $"Manifest JSON parse error: {ex.Message}"
                  }
                  );
            }
            catch (Exception ex)
            {
                return Task.FromResult
                    (
                    new CompilationResult()
                    {
                        Success = false,
                        CompilationErrors = $"Manifest parse error: {ex.Message}"
                    }
                    );
            }

            if (manifest.SampleTests.Count == 0 && !manifest.AdvancedTests.Any(t => t.LanguageCode == userSolution.LanguageCode))
                return Task.FromResult
                   (
                   new CompilationResult()
                   {
                       Success = false,
                       CompilationErrors = $"Invalid manifest: no tests for {userSolution.LanguageCode} detected"
                   }
                   );

            var fullCode = _wrapper.GenerateSource(manifest, userSolution.UserSolution, "SolutionContainer");

            File.WriteAllText(_tmpJsFilePath, fullCode);

            return Task.FromResult
                (
                new CompilationResult()
                {
                    Success = true,
                    TotalTests = manifest.SampleTests.Count + manifest.AdvancedTests.Count
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
