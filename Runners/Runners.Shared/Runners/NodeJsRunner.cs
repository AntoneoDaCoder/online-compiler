using Shared.DTOs;
using Shared.Enums;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace Runners.Shared.Runners
{
    public class NodeJsRunner : IRunner
    {
        static ProcessStartInfo _pInfo = new ProcessStartInfo()
        {
            FileName = "node",
            Arguments = "/tmp/UserProgram.js",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        readonly string[] _bannedModules = {
            // Файловая система
            "fs", "fs/promises", "path",

            // Сетевые модули
            "net", "dgram", "tls", "http", "https", "http2",

            // Дочерние процессы и управление системой
            "child_process", "cluster", "repl",

            // Модули исполнения и компиляции кода
            "vm", "eval", "async_hooks",

            // Архивы и бинарные потоки (могут читать из FS/сети)
            "zlib", "stream", "crypto",

            // Прямой доступ к модулям и системным путям
            "os", "perf_hooks",

            // Внешние процессы через URL / IPC
            "inspector", "dns", "readline", "tty",

            // Другие опасные / обходные
            "events", "util", "buffer", "console"
        };

        const string _tmpJsFilePath = "/tmp/UserProgram.js";

        const string _jsTemplate =
        """
         const bannedModules = [
          // Файловая система
          'fs', 'fs/promises', 'path',

          // Сетевые модули
          'net', 'dgram', 'tls', 'http', 'https', 'http2',

          // Дочерние процессы и управление системой
          'child_process', /* 'worker_threads', */ 'cluster', 'repl',

          // Модули исполнения и компиляции кода
          'vm', 'eval', 'async_hooks',

          // Архивы и бинарные потоки
          'zlib', 'stream', 'crypto',

          // Прямой доступ к модулям и системным путям
          'module', 'os', 'perf_hooks',

          // Внешние процессы и утилиты
          'inspector', 'dns', 'readline', 'tty',

          // Другие потенциально опасные
          'events', 'util', 'buffer', 'console'
        ];

        (function() {
          const Module = require('module');
          const originalRequire = Module.prototype.require;
          Module.prototype.require = function(moduleName) {
            if (bannedModules.includes(moduleName)) {
              throw new Error(`[SECURITY] Importing module "${moduleName}" is not allowed.`);
            }
            return originalRequire.apply(this, arguments);
          };
        })();

        const { Worker } = require('worker_threads');

        class NodeTestGenerator {
          static assertEqual(lhs, rhs, testName) {
            if (lhs === rhs) {
              console.log(`[TEST_PASS]: ${testName}`);
            } else {
              console.log(`[TEST_FAIL]: ${testName} — expected ${rhs}, got ${lhs}`);
              hasFailedTests = true;
            }
          }
          static assertGreater(lhs, rhs, testName) {
            if (lhs > rhs) {
              console.log(`[TEST_PASS]: ${testName}`);
            } else {
              console.log(`[TEST_FAIL]: ${testName} — ${rhs} is not greater than ${lhs}`);
              hasFailedTests = true;
            }
          }
          static assertApproxEqual(lhs, rhs, accuracy = 1e-6, testName) {
            if (Math.abs(lhs - rhs) <= accuracy) {
              console.log(`[TEST_PASS]: ${testName}`);
            } else {
              console.log(`[TEST_FAIL]: ${testName} — expected approx ${rhs}, got ${lhs}`);
              hasFailedTests = true;
            }
          }
        }

        function runWithTimeout(ms, fn, testName) {
          return new Promise((resolve) => {
            // код теста как строка
            const fnSource = `(${fn.toString()})();`;

            // Собираем код воркера: свой банлист, перехват require, локальный тест-раннер и ВСТАВКА пользовательского кода
            const workerCode = `
              const bannedModules = ${JSON.stringify(bannedModules)};
              (function () {
                const Module = require('module');
                const originalRequire = Module.prototype.require;
                Module.prototype.require = function (moduleName) {
                  if (bannedModules.includes(moduleName)) {
                    throw new Error('[SECURITY] Importing module "' + moduleName + '" is not allowed.');
                  }
                  return originalRequire.apply(this, arguments);
                };
              })();

              const { parentPort } = require('worker_threads');

              let _hasFailed = false;
              class NodeTestGenerator {
                static assertEqual(lhs, rhs, testName) {
                  if (lhs === rhs) {
                    console.log('[TEST_PASS]: ' + testName);
                  } else {
                    console.log('[TEST_FAIL]: ' + testName + ' — expected ' + rhs + ', got ' + lhs);
                    _hasFailed = true;
                  }
                }
                static assertGreater(lhs, rhs, testName) {
                  if (lhs > rhs) {
                    console.log('[TEST_PASS]: ' + testName);
                  } else {
                    console.log('[TEST_FAIL]: ' + testName + ' — ' + rhs + ' is not greater than ' + lhs);
                    _hasFailed = true;
                  }
                }
                static assertApproxEqual(lhs, rhs, accuracy = 1e-6, testName) {
                  if (Math.abs(lhs - rhs) <= accuracy) {
                    console.log('[TEST_PASS]: ' + testName);
                  } else {
                    console.log('[TEST_FAIL]: ' + testName + ' — expected approx ' + rhs + ', got ' + lhs);
                    _hasFailed = true;
                  }
                }
              }

              // ВСТАВКА пользовательского кода, чтобы в воркере были Solution/Item/и т.д.
              {{USER_CODE}}

              (async () => {
                try {
                  ${fnSource}
                  parentPort.postMessage({ status: 'done', failed: _hasFailed });
                } catch (err) {
                  parentPort.postMessage({ status: 'error', error: err && err.message ? err.message : String(err) });
                }
              })();
            `;

            const worker = new Worker(workerCode, { eval: true });

            const timer = setTimeout(() => {
              console.log(`[TEST_TIMED_OUT] ${testName} timed out after ${ms}ms`);
              // важно: помечаем провал в ГЛАВНОМ потоке, чтобы exitCode стал != 0
              hasFailedTests = true;
              worker.terminate();
              resolve();
            }, ms);

            worker.on('message', (msg) => {
              clearTimeout(timer);
              if (msg.status === 'done') {
                if (msg.failed) hasFailedTests = true;
                resolve();
              } else if (msg.status === 'error') {
                console.log(`[TEST_FAIL]: ${testName} — Runtime error: ${msg.error}`);
                hasFailedTests = true;
                resolve();
              }
            });

            worker.on('error', (err) => {
              clearTimeout(timer);
              console.log(`[TEST_FAIL]: ${testName} — Worker error: ${err && err.message ? err.message : String(err)}`);
              hasFailedTests = true;
              resolve();
            });
          });
        }

        let hasFailedTests = false;

        {{USER_CODE}}

        (async () => {
          {{TESTS}}

          if (hasFailedTests) {
            process.exitCode = 1;
          }
        })();
        
        """;

        const int _maxProcessLifetime = 25000;

        bool _isDisposed;

        public async Task<CodeResponseDto> ExecuteCodeAsync(Guid requestId, DateTime requestDate, CancellationToken cancellationToken)
        {
            var result = new CodeResponseDto()
            {
                RequestId = requestId,
                Language = "nodejs",
                Result = new ExecutionResultDto()
                {
                    RequestSentAt = requestDate,
                }
            };

            using var proc = new Process() { StartInfo = _pInfo };
            proc.Start();

            if (!proc.WaitForExit(_maxProcessLifetime))
            {
                proc.Kill();
                result.Result.Status = ExecutionStatus.TimedOut;
                result.Result.ExitCode = 124;
                result.Result.ConsoleOutput = "Execution timed out.";
                return result;
            }
            result.Result.ExitCode = proc.ExitCode;

            if (proc.ExitCode != 0)
            {
                result.Status = RequestStatus.Failed;
                result.Result.Status = ExecutionStatus.RuntimeError;

                string stdOut = await proc.StandardOutput.ReadToEndAsync(cancellationToken);
                string errorString = await proc.StandardError.ReadToEndAsync(cancellationToken);

                result.Result.ExitCode = proc.ExitCode;

                if (proc.ExitCode != 0)
                {
                    result.Status = RequestStatus.Failed;

                    if (stdOut.Contains("[TEST_TIMED_OUT]"))
                    {
                        result.Result.Status = ExecutionStatus.TimedOut;

                        var failedTestNames = new StringBuilder();
                        var lines = stdOut.Split('\n', StringSplitOptions.RemoveEmptyEntries);

                        foreach (var line in lines)
                        {
                            if (line.StartsWith("[TEST_TIMED_OUT]"))
                            {
                                var testNameMatch = Regex.Match(line, @"\[TEST_TIMED_OUT\]\s*(.*?)\s*timed out");
                                if (testNameMatch.Success)
                                    failedTestNames.AppendLine($"{testNameMatch.Groups[1].Value.Trim()} (timed out)");
                            }
                        }

                        result.Result.ConsoleOutput = failedTestNames.Length > 0
                            ? failedTestNames.ToString()
                            : stdOut;
                    }
                    else if (stdOut.Contains("[TEST_FAIL]:"))
                    {
                        result.Result.Status = ExecutionStatus.FailedToExecute;

                        var failedTestNames = new StringBuilder();
                        var lines = stdOut.Split('\n', StringSplitOptions.RemoveEmptyEntries);

                        foreach (var line in lines)
                        {
                            if (line.StartsWith("[TEST_FAIL]:"))
                            {
                                var testNameMatch = Regex.Match(line, @"\[TEST_FAIL\]:\s*(.*?)\s*(?:—|$)");
                                if (testNameMatch.Success)
                                    failedTestNames.AppendLine(testNameMatch.Groups[1].Value.Trim());
                            }
                        }

                        result.Result.ConsoleOutput = failedTestNames.Length > 0
                            ? failedTestNames.ToString()
                            : stdOut;
                    }
                    else
                    {
                        result.Result.Status = ExecutionStatus.RuntimeError;
                        result.Result.ConsoleOutput =
                            (!string.IsNullOrWhiteSpace(stdOut) ? stdOut + Environment.NewLine : "")
                            + (!string.IsNullOrWhiteSpace(errorString) ? errorString : "");
                    }
                }
                else
                {
                    result.Status = RequestStatus.Succeeded;
                    result.Result.Status = ExecutionStatus.Succeeded;
                    result.Result.ConsoleOutput = stdOut;
                }

            }
            else
            {
                result.Status = RequestStatus.Succeeded;
                result.Result.Status = ExecutionStatus.Succeeded;

                Console.WriteLine("[NodeRunner] Successfully executed");
            }

            File.Delete(_tmpJsFilePath);

            return result;
        }

        public Task<(bool Success, string CompilationErrors)> CompileCodeAsync(string fullCode, CancellationToken cancellationToken)
        {
            foreach (var pattern in _bannedModules)
            {
                if (Regex.IsMatch(fullCode, $@"require\(['""]{pattern}['""]\)"))
                {
                    return Task.FromResult((false, $"Banned import detected: {pattern}"));
                }
                if (Regex.IsMatch(fullCode, $@"import\s+.*\s+from\s+['""]{pattern}['""]"))
                {
                    return Task.FromResult((false, $"Banned import detected: {pattern}"));
                }
            }

            File.WriteAllText(_tmpJsFilePath, fullCode);

            return Task.FromResult((true, string.Empty));
        }

        public string WrapCode(ProblemSolutionDto problemSolutionDto)
        {
            var mainBody = new StringBuilder(_jsTemplate);

            var defsBuilder = new StringBuilder();
            foreach (var definition in problemSolutionDto.Problem.AdditionalDefinitions)
                defsBuilder.AppendLine(definition.Value);

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
