using Shared.DTOs;
using Shared.Enums;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace Runners.Shared.Runners
{
    public class TypeScriptRunner : IRunner
    {
        const string _tmpTsFilePath = "/tmp/UserProgram.ts";
        const string _tmpJsFilePath = "/tmp/UserProgram.js";
        const int _maxProcessLifeTime = 25000;

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

        readonly string[] _bannedModules = {
            "fs", "fs/promises", "path",

            "net", "dgram", "tls", "http", "https", "http2",

            "child_process", "cluster", "repl",

            "vm", "eval", "async_hooks",

            "zlib", "stream", "crypto",

            "os", "perf_hooks",

            "inspector", "dns", "readline", "tty",

            "events", "util", "buffer", "console"
        };

        const string _tsTemplate =
        """
        declare const process: { exitCode?: number };

        class NodeTestGenerator {
            static assertEqual(lhs: any, rhs: any, testName: string): void {
                if (lhs === rhs) {
                    console.log(`[TEST_PASS]: ${testName}`);
                } else {
                    console.log(`[TEST_FAIL]: ${testName} — expected ${rhs}, got ${lhs}`);
                    hasFailedTests = true;
                }
            }
    
            static assertGreater(lhs: number, rhs: number, testName: string): void {
                if (lhs > rhs) {
                    console.log(`[TEST_PASS]: ${testName}`);
                } else {
                    console.log(`[TEST_FAIL]: ${testName} — ${rhs} is not greater than ${lhs}`);
                    hasFailedTests = true;
                }
            }
    
            static assertApproxEqual(lhs: number, rhs: number, accuracy: number = 1e-6, testName: string): void {
                if (Math.abs(lhs - rhs) <= accuracy) {
                    console.log(`[TEST_PASS]: ${testName}`);
                } else {
                    console.log(`[TEST_FAIL]: ${testName} — expected approx ${rhs}, got ${lhs}`);
                    hasFailedTests = true;
                }
            }
        }

        async function runWithTimeout(ms: number, fn: () => Promise<void>, testName: string): Promise<void> {
            return new Promise((resolve) => {
                const timer = setTimeout(() => {
                    console.log(`[TEST_TIMED_OUT] ${testName} timed out after ${ms}ms`);
                    hasFailedTests = true;
                    resolve();
                }, ms);

                (async () => {
                    try {
                        await fn();
                        clearTimeout(timer);
                        resolve();
                    } catch (err) {
                        clearTimeout(timer);
                        console.log(`[TEST_FAIL]: ${testName} — Runtime error: ${err && err.message ? err.message : String(err)}`);
                        hasFailedTests = true;
                        resolve();
                    }
                })();
            });
        }

        let hasFailedTests: boolean = false;

        {{USER_CODE}}

        (async () => {
            {{TESTS}}

            if (hasFailedTests) {
                process.exitCode = 1;
            }
        })();
        """;

        public async Task<(bool Success, string CompilationErrors)> CompileCodeAsync(string fullCode, CancellationToken cancellationToken)
        {
            foreach (var pattern in _bannedModules)
            {
                if (Regex.IsMatch(fullCode, $@"require\(['""]{pattern}['""]\)"))
                {
                    return (false, $"Banned import detected: {pattern}");
                }

                if (Regex.IsMatch(fullCode, $@"import\s+.*\s+from\s+['""]{pattern}['""]"))
                {
                    return (false, $"Banned import detected: {pattern}");
                }
            }

            File.WriteAllText(_tmpTsFilePath, fullCode);

            using var process = new Process() { StartInfo = _tsInfo };
            process.Start();

            if (!process.WaitForExit(_maxProcessLifeTime))
            {
                process.Kill();

                File.Delete(_tmpTsFilePath);

                return (false, "TypeScript compilation timed out.");
            }

            var compilationErrors = await process.StandardError.ReadToEndAsync(cancellationToken);
            var compilationOutput = await process.StandardOutput.ReadToEndAsync(cancellationToken);

            if (process.ExitCode != 0)
            {
                File.Delete(_tmpTsFilePath);

                return (false, $"TypeScript compilation failed: {compilationErrors}{compilationOutput}");
            }

            File.Delete(_tmpTsFilePath);

            return (true, string.Empty);
        }

        public async Task<CodeResponseDto> ExecuteCodeAsync(Guid requestId, DateTime requestDate, CancellationToken cancellationToken)
        {
            var result = new CodeResponseDto()
            {
                RequestId = requestId,
                Language = "typescript",
                Result = new ExecutionResultDto()
                {
                    RequestSentAt = requestDate
                }
            };

            using var process = new Process() { StartInfo = _nodeInfo };
            process.Start();

            if (!process.WaitForExit(_maxProcessLifeTime))
            {
                process.Kill();
                result.Result.Status = ExecutionStatus.TimedOut;
                result.Result.ExitCode = 124;
                result.Result.ConsoleOutput = "Execution timed out.";

                File.Delete(_tmpJsFilePath);

                return result;
            }

            result.Result.ExitCode = process.ExitCode;

            if (process.ExitCode != 0)
            {
                result.Status = RequestStatus.Failed;
                result.Result.Status = ExecutionStatus.RuntimeError;

                string errorString = await process.StandardError.ReadToEndAsync(cancellationToken);
                string stdOut = await process.StandardOutput.ReadToEndAsync(cancellationToken);

                if (stdOut.Contains("[TEST_TIMED_OUT]"))
                {
                    result.Result.Status = ExecutionStatus.TimedOut;
                }
                else if (stdOut.Contains("[TEST_FAIL]:"))
                {
                    result.Result.Status = ExecutionStatus.FailedToExecute;

                    var failedTestNames = new StringBuilder();
                    var lines = stdOut.Split('\n');

                    foreach (var line in lines)
                    {
                        if (line.StartsWith("[TEST_FAIL]: "))
                        {
                            var testNameMatch = Regex.Match(line, @"\[TEST_FAIL\]:\s*(.*?)\s*—");

                            if (testNameMatch.Success)
                            {
                                failedTestNames.AppendLine(testNameMatch.Groups[1].Value);
                            }
                        }
                    }

                    result.Result.ConsoleOutput = failedTestNames.ToString();
                }
                else
                {
                    result.Result.Status = ExecutionStatus.RuntimeError;

                    result.Result.ConsoleOutput = !string.IsNullOrWhiteSpace(errorString)
                                                  ? errorString
                                                  : stdOut;
                }

                Console.WriteLine("[TypeScriptRunner] Failed to execute, errors:" + result.Result.ConsoleOutput);
            }
            else
            {
                result.Status = RequestStatus.Succeeded;
                result.Result.Status = ExecutionStatus.Succeeded;

                Console.WriteLine("[TypeScriptRunner] Successfully executed");
            }

            File.Delete(_tmpTsFilePath);
            File.Delete(_tmpJsFilePath);

            return result;
        }

        public string WrapCode(ProblemSolutionDto problemSolutionDto)
        {
            var mainBody = new StringBuilder(_tsTemplate);
            var defsBuilder = new StringBuilder();

            foreach (var definition in problemSolutionDto.Problem.AdditionalDefinitions)
            {
                defsBuilder.AppendLine(definition.Value);
            }

            mainBody = mainBody.Replace("{{USER_CODE}}", defsBuilder + problemSolutionDto.Code);

            var testBuilder = new StringBuilder();

            foreach (var testCase in problemSolutionDto.Problem.TestCases)
            {
                testBuilder.AppendLine($@"
                await runWithTimeout({problemSolutionDto.MaxAllowedTimeInMilliseconds}, async () => {{
                    {testCase.TestInitialization}
                    {testCase.InputExpression}
                    {testCase.OutputExpression}
                }}, '{testCase.Name}');
                ");
            }

            mainBody = mainBody.Replace("{{TESTS}}", testBuilder.ToString());

            return mainBody.ToString();
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
    }
}
