using ServerAPIApp.Domain.Exceptions.BadRequestExceptions;
using Shared.DTOs;
using Shared.Enums;
using Shared.Helpers;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace Runners.Shared.Runners
{
    public partial class TypeScriptRunner : IRunner
    {
        const string _basePath = "/tmp";
        const int _maxProcessLifetime = 25000;

        private readonly ITestWrapper _codeWrapper;
        private bool _isDisposed;

        public TypeScriptRunner(ITestWrapper wrapper)
        {
            _codeWrapper = wrapper;
        }

        public async Task<CompilationResult> CompileCodeAsync(ProblemSolutionDto userSolution, CancellationToken cancellationToken = default)
        {
            ManifestDto manifest;
            try
            {
                manifest = ManifestParser.Parse(userSolution.TestManifestJson, userSolution.LanguageCode);
            }
            catch (InvalidTestTemplateException ex)
            {
                return new CompilationResult()
                {
                    Success = false,
                    CompilationErrors = $"Manifest validation failed: {ex.Message}",
                    TotalTests = 0,
                    ExecutablePath = string.Empty
                };
            }
            catch (System.Text.Json.JsonException ex)
            {
                return new CompilationResult()
                {
                    Success = false,
                    CompilationErrors = $"Manifest JSON parse error: {ex.Message}",
                    TotalTests = 0,
                    ExecutablePath = string.Empty
                };
            }
            catch (Exception ex)
            {
                return new CompilationResult()
                {
                    Success = false,
                    CompilationErrors = $"Manifest parse error: {ex.Message}",
                    TotalTests = 0,
                    ExecutablePath = string.Empty
                };
            }

            if (manifest.SampleTests.Count == 0 && !manifest.AdvancedTests.Any(t => t.LanguageCode == userSolution.LanguageCode))
            {
                return new CompilationResult()
                {
                    Success = false,
                    CompilationErrors = $"Invalid manifest: no tests for {userSolution.LanguageCode} detected",
                    TotalTests = 0,
                    ExecutablePath = string.Empty
                };
            }

            var baseName = userSolution.RequestId.ToString("N");
            var tsPath = Path.Combine(_basePath, $"{baseName}.ts");
            var jsPath = Path.Combine(_basePath, $"{baseName}.js");

            var fullCode = _codeWrapper.GenerateSource(manifest, userSolution.UserSolution, "SolutionContainer");
            File.WriteAllText(tsPath, fullCode);

            var pInfo = new ProcessStartInfo
            {
                FileName = "tsc",
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };

            pInfo.ArgumentList.Add("--target");
            pInfo.ArgumentList.Add("ES2020");
            pInfo.ArgumentList.Add("--module");
            pInfo.ArgumentList.Add("CommonJS");
            pInfo.ArgumentList.Add("--outDir");
            pInfo.ArgumentList.Add(_basePath);
            pInfo.ArgumentList.Add(tsPath);

            using var process = new Process { StartInfo = pInfo };

            try
            {
                process.Start();

                var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
                var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);

                if (!process.WaitForExit(_maxProcessLifetime))
                {
                    try
                    {
                        process.Kill(entireProcessTree: true);
                    }
                    catch
                    {
                    }

                    return new CompilationResult()
                    {
                        Success = false,
                        CompilationErrors = "TypeScript compilation timed out.",
                        TotalTests = 0,
                        ExecutablePath = string.Empty
                    };
                }

                await Task.WhenAll(stdoutTask, stderrTask);

                var compilationOutput = stdoutTask.Result ?? string.Empty;
                var compilationErrors = stderrTask.Result ?? string.Empty;

                if (process.ExitCode != 0)
                {
                    return new CompilationResult()
                    {
                        Success = false,
                        CompilationErrors = $"TypeScript compilation failed: {compilationErrors}{compilationOutput}",
                        TotalTests = 0,
                        ExecutablePath = string.Empty
                    };
                }

                return new CompilationResult()
                {
                    Success = true,
                    TotalTests = manifest.SampleTests.Count + manifest.AdvancedTests.Count,
                    ExecutablePath = jsPath,
                    CompilationErrors = null
                };
            }
            finally
            {
                try
                {
                    if (File.Exists(tsPath))
                    {
                        File.Delete(tsPath);
                    }
                }
                catch
                {
                }
            }
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
                    passedMatch.Success && int.TryParse(passedMatch.Groups[1].Value, out var passedCount)
                        ? passedCount
                        : 0;

                if (proc.ExitCode == 0)
                {
                    result.Status = RequestStatus.Succeeded;
                    result.Result.Status = ExecutionStatus.Succeeded;
                    result.Result.ConsoleOutput = stdOut.Trim();
                }
                else
                {
                    result.Status = RequestStatus.Failed;
                    result.Result.Status = ExecutionStatus.RuntimeError;

                    var failedMatches = FailedTestsRegex().Matches(stdOut ?? string.Empty);

                    var failedDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    foreach (Match m in failedMatches)
                    {
                        if (!m.Success)
                        {
                            continue;
                        }

                        var name = m.Groups[1].Value.Trim();
                        var reason = m.Groups[2].Value.Trim()
                            .Replace("\r", "")
                            .Replace("\n", " ")
                            .Trim();

                        if (!failedDict.ContainsKey(name))
                        {
                            failedDict[name] = reason;
                        }
                    }

                    var stderrLines = (stdErr ?? string.Empty).Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

                    var detailMap = new Dictionary<string, StringBuilder>(StringComparer.OrdinalIgnoreCase);
                    for (int i = 0; i < stderrLines.Length; i++)
                    {
                        var line = stderrLines[i];
                        var m = DetailRegex().Match(line);
                        if (!m.Success)
                        {
                            continue;
                        }

                        var currentKey = m.Groups[1].Value.Trim();
                        var rest = m.Groups[2].Value ?? string.Empty;

                        if (!detailMap.TryGetValue(currentKey, out var sb))
                        {
                            sb = new StringBuilder();
                            detailMap[currentKey] = sb;
                        }

                        if (!string.IsNullOrWhiteSpace(rest))
                        {
                            sb.AppendLine(rest.Trim());
                        }

                        int j = i + 1;
                        while (j < stderrLines.Length &&
                               (stderrLines[j].StartsWith(" ") ||
                                stderrLines[j].StartsWith('\t') ||
                                stderrLines[j].StartsWith("at ")))
                        {
                            sb.AppendLine(stderrLines[j].Trim());
                            j++;
                        }

                        i = j - 1;
                    }

                    var failedList = new List<(string TestName, string Reason, string Detail)>();
                    foreach (var kv in failedDict)
                    {
                        var name = kv.Key;
                        var reason = kv.Value;
                        var detail = detailMap.TryGetValue(name, out var sb) ? sb.ToString().Trim() : string.Empty;
                        failedList.Add((name, reason, detail));
                    }

                    if (failedList.Count > 0)
                    {
                        bool anyTimeout = failedList.Any(f =>
                            (f.Reason?.IndexOf("timed out", StringComparison.OrdinalIgnoreCase) >= 0) ||
                            (f.Reason?.IndexOf("timeout", StringComparison.OrdinalIgnoreCase) >= 0) ||
                            (f.Detail?.IndexOf("timed out", StringComparison.OrdinalIgnoreCase) >= 0) ||
                            (f.Detail?.IndexOf("timeout", StringComparison.OrdinalIgnoreCase) >= 0));

                        result.Result.Status = anyTimeout ? ExecutionStatus.TimedOut : ExecutionStatus.FailedToExecute;

                        var sbOut = new StringBuilder();
                        sbOut.AppendLine("Failed tests:");
                        foreach (var f in failedList)
                        {
                            if (!string.IsNullOrEmpty(f.Detail))
                            {
                                sbOut.AppendLine($"{f.TestName} - {f.Reason}");
                                sbOut.AppendLine("  Details:");
                                foreach (var dl in f.Detail.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries))
                                {
                                    sbOut.AppendLine("    " + dl);
                                }
                            }
                            else
                            {
                                sbOut.AppendLine($"{f.TestName} - {f.Reason}");
                            }
                        }

                        sbOut.AppendLine();
                        sbOut.AppendLine("--- STDOUT ---");
                        sbOut.AppendLine(stdOut.Trim());
                        sbOut.AppendLine();
                        sbOut.AppendLine("--- STDERR ---");
                        sbOut.AppendLine(stdErr.Trim());

                        result.Result.ConsoleOutput = sbOut.ToString().Trim();
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

        [GeneratedRegex(@"FailedTest:([^:]+):([^\r\n]*)", RegexOptions.IgnoreCase)]
        private static partial Regex FailedTestsRegex();

        [GeneratedRegex(@"\[FAILED-DETAIL\]\s*([^:]+):\s*(.*)", RegexOptions.IgnoreCase)]
        private static partial Regex DetailRegex();
    }
}