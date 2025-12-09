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
        const string _tmpTsFilePath = "/tmp/UserProgram.ts";
        const string _tmpJsFilePath = "/tmp/UserProgram.js";

        const int _maxProcessLifetime = 25000;

        private ITestWrapper _codeWrapper;

        bool _isDisposed;

        ProcessStartInfo _tsInfo = new ProcessStartInfo()
        {
            FileName = "npx",
            Arguments = "tsc --target ES2020 --module CommonJS --outDir /tmp /tmp/UserProgram.ts",
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
        };

        ProcessStartInfo _nodeInfo = new ProcessStartInfo()
        {
            FileName = "node",
            Arguments = "/tmp/UserProgram.js",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

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
                    CompilationErrors = $"Manifest validation failed: {ex.Message}"
                };
            }
            catch (System.Text.Json.JsonException ex)
            {
                return new CompilationResult()
                {
                    Success = false,
                    CompilationErrors = $"Manifest JSON parse error: {ex.Message}"
                };
            }
            catch (Exception ex)
            {
                return new CompilationResult()
                {
                    Success = false,
                    CompilationErrors = $"Manifest parse error: {ex.Message}"
                };
            }

            if (manifest.SampleTests.Count == 0 && !manifest.AdvancedTests.Any(t => t.LanguageCode == userSolution.LanguageCode))
                return
                   new CompilationResult()
                   {
                       Success = false,
                       CompilationErrors = $"Invalid manifest: no tests for {userSolution.LanguageCode} detected"
                   };

            var fullCode = _codeWrapper.GenerateSource(manifest, userSolution.UserSolution, "SolutionContainer");


            File.WriteAllText(_tmpTsFilePath, fullCode);

            using var process = new Process() { StartInfo = _tsInfo };
            process.Start();

            if (!process.WaitForExit(_maxProcessLifetime))
            {
                process.Kill();

                File.Delete(_tmpTsFilePath);

                return new CompilationResult()
                {
                    Success = false,
                    CompilationErrors = "TypeScript compilation timed out."
                };
            }

            var compilationErrors = await process.StandardError.ReadToEndAsync(cancellationToken);
            var compilationOutput = await process.StandardOutput.ReadToEndAsync(cancellationToken);

            if (process.ExitCode != 0)
            {
                File.Delete(_tmpTsFilePath);

                return new CompilationResult()
                {
                    Success = false,
                    CompilationErrors = $"TypeScript compilation failed: {compilationErrors}{compilationOutput}"
                };
            }

            File.Delete(_tmpTsFilePath);

            return new CompilationResult()
            {
                Success = true,
                TotalTests = manifest.SampleTests.Count + manifest.AdvancedTests.Count
            };
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
                StartInfo = _nodeInfo,
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
                File.Delete(_tmpTsFilePath);

                return result;
            }

            result.Result.ExitCode = proc.ExitCode;

            var stdOut = await proc.StandardOutput.ReadToEndAsync(cancellationToken) ?? string.Empty;
            var stdErr = await proc.StandardError.ReadToEndAsync(cancellationToken) ?? string.Empty;

            // 1) PassedTests:<n>
            int passedCount = 0;
            var passedMatch = PassedTestsRegex().Match(stdOut);
            if (passedMatch.Success && int.TryParse(passedMatch.Groups[1].Value, out var p))
                passedCount = p;
            result.Result.PassedTests = passedCount;

            // If exit code == 0 => success
            if (proc.ExitCode == 0)
            {
                result.Status = RequestStatus.Succeeded;
                result.Result.Status = ExecutionStatus.Succeeded;
                // keep stdout for diagnostics
                result.Result.ConsoleOutput = stdOut?.Trim() ?? string.Empty;
            }
            else
            {
                // default
                result.Status = RequestStatus.Failed;
                result.Result.Status = ExecutionStatus.RuntimeError;

                // 2) parse FailedTest markers from stdout (one-line reasons)
                // pattern: FailedTest:TestName:Reason  (Reason is single-line, no newlines)
                var failedMatches = FailedTestsRegex().Matches(stdOut ?? string.Empty);

                // collect unique failures preserving first-seen reason
                var failedDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (Match m in failedMatches)
                {
                    if (!m.Success) continue;
                    var name = m.Groups[1].Value.Trim();
                    var reason = m.Groups[2].Value.Trim().Replace("\r", "").Replace("\n", " ").Trim();
                    if (!failedDict.ContainsKey(name))
                        failedDict[name] = reason;
                }

                // 3) parse detailed stderr blocks like:
                // [FAILED-DETAIL] TestName: <stack or multiline info>
                // We'll capture each line that starts with [FAILED-DETAIL] and then take the rest of the line.

                // Because stack traces may be multiline, we also capture any subsequent lines up to next [FAILED-DETAIL] marker
                // Simpler approach: split stderr by lines and group lines starting with marker
                var stderrLines = (stdErr ?? string.Empty).Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

                var detailMap = new Dictionary<string, StringBuilder>(StringComparer.OrdinalIgnoreCase);
                string currentKey = null;

                for (int i = 0; i < stderrLines.Length; i++)
                {
                    var line = stderrLines[i];
                    var m = DetailRegex().Match(line);
                    if (m.Success)
                    {
                        currentKey = m.Groups[1].Value.Trim();
                        var rest = m.Groups[2].Value ?? "";
                        if (!detailMap.TryGetValue(currentKey, out var sb)) { sb = new StringBuilder(); detailMap[currentKey] = sb; }
                        if (rest.Length > 0) sb.AppendLine(rest.Trim());

                        int j = i + 1;
                        while (j < stderrLines.Length && (stderrLines[j].StartsWith(" ") || stderrLines[j].StartsWith('\t') || stderrLines[j].StartsWith("at ")))
                        {
                            sb.AppendLine(stderrLines[j].Trim());
                            j++;
                        }
                        // continue loop from j-1
                        i = j - 1;
                    }
                    else
                    {
                        // nothing — ignore other stderr lines (they will be included in final dump)
                    }
                }

                var failedList = new List<(string TestName, string Reason, string Detail)>();
                foreach (var kv in failedDict)
                {
                    var name = kv.Key;
                    var reason = kv.Value;
                    var detail = detailMap.TryGetValue(name, out var sb) ? sb.ToString().Trim() : string.Empty;
                    failedList.Add((name, reason, detail));
                }

                int failedCount = failedList.Count;

                if (failedCount > 0)
                {
                    // decide if any timeout present (look both at short reason and detail text)
                    bool anyTimeout = failedList.Any(f =>
                        (f.Reason?.IndexOf("timed out", StringComparison.OrdinalIgnoreCase) >= 0) ||
                        (f.Reason?.IndexOf("timeout", StringComparison.OrdinalIgnoreCase) >= 0) ||
                        (f.Detail?.IndexOf("timed out", StringComparison.OrdinalIgnoreCase) >= 0) ||
                        (f.Detail?.IndexOf("timeout", StringComparison.OrdinalIgnoreCase) >= 0)
                    );

                    result.Result.Status = anyTimeout ? ExecutionStatus.TimedOut : ExecutionStatus.FailedToExecute;

                    // Build ConsoleOutput: failed list + optional details + stdout/stderr dumps
                    var sbOut = new StringBuilder();
                    sbOut.AppendLine("Failed tests:");
                    foreach (var f in failedList)
                    {
                        if (!string.IsNullOrEmpty(f.Detail))
                        {
                            sbOut.AppendLine($"{f.TestName} - {f.Reason}");
                            sbOut.AppendLine("  Details:");
                            foreach (var dl in f.Detail.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries))
                                sbOut.AppendLine("    " + dl);
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
                    // no FailedTest markers in stdout — fallback heuristics using combined outputs
                    var combined = (stdOut + "\n" + stdErr) ?? string.Empty;
                    if (combined.Contains("timed out", StringComparison.OrdinalIgnoreCase)
                        || combined.Contains("timeout", StringComparison.OrdinalIgnoreCase))
                        result.Result.Status = ExecutionStatus.TimedOut;
                    else if (combined.Contains("assert", StringComparison.OrdinalIgnoreCase)
                             || combined.Contains("failed", StringComparison.OrdinalIgnoreCase))
                        result.Result.Status = ExecutionStatus.FailedToExecute;
                    else
                        result.Result.Status = ExecutionStatus.RuntimeError;

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
            File.Delete(_tmpTsFilePath);

            return result;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed) return;

            if (disposing)
            {
                if (File.Exists(_tmpTsFilePath))
                    File.Delete(_tmpTsFilePath);

                if (File.Exists(_tmpJsFilePath))
                    File.Delete(_tmpJsFilePath);
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
