using Shared.DTOs;
using Shared.Enums;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace Runners.Shared.Runners
{
    public class SwiftRunner : IRunner
    {
        const string _tmpSwiftFilePath = "/tmp/UserProgram.swift";
        const string _tmpSwiftBinaryPath = "/tmp/UserProgram";
        const int _maxProcessLifetime = 25000;

        static ProcessStartInfo _compilePInfo = new ProcessStartInfo()
        {
            FileName = "swiftc",
            Arguments = $"-O -gnone -whole-module-optimization \"{_tmpSwiftFilePath}\" -o \"{_tmpSwiftBinaryPath}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };


        static ProcessStartInfo _pInfo = new ProcessStartInfo()
        {
            FileName = _tmpSwiftBinaryPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        private bool _isDisposed;

        public string WrapCode(ProblemSolutionDto problemSolutionDto)
        {
            var sb = new StringBuilder();

            sb.AppendLine(
                $$"""
        import Foundation
        import Dispatch
        import Glibc

        signal(SIGABRT) { _ in fputs("TEST_ERROR: signal SIGABRT\n", stderr); exit(1) }
        signal(SIGSEGV) { _ in fputs("TEST_ERROR: signal SIGSEGV\n", stderr); exit(1) }
        signal(SIGILL)  { _ in fputs("TEST_ERROR: signal SIGILL\n", stderr); exit(1) }
        signal(SIGFPE)  { _ in fputs("TEST_ERROR: signal SIGFPE\n", stderr); exit(1) }

        final class SwiftTestGenerator {
            static func assertEqual<T: Equatable>(_ lhs: T, _ rhs: T, testName: String) {
                if lhs == rhs {
                    print("[TEST_PASS]: \(testName)")
                } else {
                    print("[TEST_FAIL]: \(testName) — expected \(rhs), got \(lhs)")
                    hasFailedTests = true
                }
            }

            static func assertGreater<T: Comparable>(_ lhs: T, _ rhs: T, testName: String) {
                if lhs > rhs {
                    print("[TEST_PASS]: \(testName)")
                } else {
                    print("[TEST_FAIL]: \(testName) — \(rhs) is not greater than \(lhs)")
                    hasFailedTests = true
                }
            }

            static func assertApproxEqual(_ lhs: Double, _ rhs: Double, accuracy: Double = 1e-6, testName: String) {
                if abs(lhs - rhs) <= accuracy {
                    print("[TEST_PASS]: \(testName)")
                } else {
                    print("[TEST_FAIL]: \(testName) — expected approx \(rhs), got \(lhs)")
                    hasFailedTests = true
                }
            }
        }
        """
        );


            foreach (var definition in problemSolutionDto.Problem.AdditionalDefinitions)
                sb.AppendLine(definition.Value);
            sb.AppendLine(
                $$"""
                {{problemSolutionDto.Code}}
                

                func runWithTimeout(seconds: Double, task: @escaping () -> Void) -> Bool {
                let group = DispatchGroup()
                group.enter()

                DispatchQueue.global().async {
                    task()
                    group.leave()
                }

                let result = group.wait(timeout: .now() + seconds)
                return result == .success
            }

            var hasFailedTests = false

            func runTests() {
            """
            );

            double timeoutSeconds = Math.Max(0.1, problemSolutionDto.MaxAllowedTimeInMilliseconds / 1000.0);

            foreach (var testCase in problemSolutionDto.Problem.TestCases)
            {
                sb.AppendLine(
                    $$"""
                        let {{testCase.Name}}_success = runWithTimeout(seconds: {{timeoutSeconds}}) {
                        {{testCase.TestInitialization}}

                        {{testCase.InputExpression}}

                        {{testCase.OutputExpression}}
                    }

                    if !{{testCase.Name}}_success {
                        print("[TEST_TIMED_OUT] {{testCase.Name}} timed out after {{timeoutSeconds}}s")
                        exit(124)
                    }
                    """
                );
            }

            sb.AppendLine(
            """
                if hasFailedTests {
                    exit(1)
                }
            }
            """
            );

            sb.AppendLine("runTests()");
            return sb.ToString();
        }


        public async Task<(bool Success, string CompilationErrors)> CompileCodeAsync(string fullCode, CancellationToken cancellationToken)
        {
            File.WriteAllText(_tmpSwiftFilePath, fullCode);

            var compile = new Process
            {
                StartInfo = _compilePInfo
            };

            compile.Start();

            string compileStderr = await compile.StandardError.ReadToEndAsync(cancellationToken);

            compile.WaitForExit();

            File.Delete(_tmpSwiftFilePath);

            if (compile.ExitCode != 0)
            {
                Console.WriteLine("[SwiftRunner] Compiled code with errors: " + compileStderr);
                return (false, compileStderr);
            }
            else
            {
                Console.WriteLine("[SwiftRunner] Successfully compiled code");
                return (true, string.Empty);
            }
        }

        public async Task<CodeResponseDto> ExecuteCodeAsync(Guid requestId, DateTime requestDate, CancellationToken cancellationToken)
        {
            var result = new CodeResponseDto()
            {
                RequestId = requestId,
                Language = "swift",
                Result = new ExecutionResultDto()
                {
                    RequestSentAt = requestDate,
                }
            };

            using var proc = new Process
            {
                StartInfo = _pInfo,
            };


            Console.WriteLine("[SwiftRunner] Launched process");
            proc.Start();


            if (!proc.WaitForExit(_maxProcessLifetime))
            {
                proc.Kill();
                result.Status = RequestStatus.Failed;
                result.Result.Status = ExecutionStatus.TimedOut;
                result.Result.ExitCode = 124;
                result.Result.ConsoleOutput = "Execution timed out.";

                File.Delete(_tmpSwiftBinaryPath);

                Console.WriteLine("[SwiftRunner] Process timed out");

                return result;
            }

            result.Result.ExitCode = proc.ExitCode;

            if (proc.ExitCode != 0)
            {
                result.Status = RequestStatus.Failed;
                result.Result.Status = ExecutionStatus.RuntimeError;

                string errorString = await proc.StandardError.ReadToEndAsync(cancellationToken);
                string stdOut = await proc.StandardOutput.ReadToEndAsync(cancellationToken);

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
                        if (line.StartsWith("[TEST_FAIL]:"))
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

                    result.Result.ConsoleOutput = errorString;
                }

                Console.WriteLine("[SwiftRunner] Failed to execute, errors:" + result.Result.ConsoleOutput);
            }
            else
            {
                result.Status = RequestStatus.Succeeded;
                result.Result.Status = ExecutionStatus.Succeeded;

                Console.WriteLine("[SwiftRunner] Successfully executed");
            }

            File.Delete(_tmpSwiftBinaryPath);

            return result;
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
