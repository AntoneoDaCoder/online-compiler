using Shared.DTOs;
using System.Text;
using System.Text.RegularExpressions;

namespace Runners.Shared.Runners
{
    public class NodeJsRunner : IRunner
    {
        readonly string[] _bannedModules = {
            // Файловая система
            "fs", "fs/promises", "path",

            // Сетевые модули
            "net", "dgram", "tls", "http", "https", "http2",

            // Дочерние процессы и управление системой
            "child_process", "worker_threads", "cluster", "repl",

            // Модули исполнения и компиляции кода
            "vm", "eval", "async_hooks",

            // Архивы и бинарные потоки (могут читать из FS/сети)
            "zlib", "stream", "crypto",

            // Прямой доступ к модулям и системным путям
            "module", "os", "perf_hooks",

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
          'child_process', 'worker_threads', 'cluster', 'repl',

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

        class Test {
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
            let finished = false;
            const timer = setTimeout(() => {
              if (!finished) {
                console.log(`[TEST_TIMED_OUT] ${testName} timed out after ${ms}ms`);
                process.exit(124);
              }
            }, ms);

            try {
              Promise.resolve(fn()).finally(() => {
                finished = true;
                clearTimeout(timer);
                resolve();
              });
            } catch (err) {
              finished = true;
              clearTimeout(timer);
              throw err;
            }
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


        public async Task<CodeResponseDto> ExecuteCodeAsync(Guid requestId, DateTime requestDate, CancellationToken cancellationToken)
        {

        }

        public Task<(bool Success, string CompilationErrors)> CompileCodeAsync(string fullCode, CancellationToken cancellationToken)
        {
            foreach (var pattern in _bannedModules)
            {
                if (Regex.IsMatch(fullCode, pattern))
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

            mainBody = mainBody.Replace("{{USER_CODE}}", problemSolutionDto.Code);

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
    }
}
