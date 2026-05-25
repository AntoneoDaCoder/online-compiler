using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;
using NUnitLite;
using ServerAPIApp.Domain.Exceptions.BadRequestExceptions;
using Shared.DTOs;
using Shared.Enums;
using Shared.Helpers;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;

namespace Runners.Shared.Runners
{
    public class DotNetRunner : IRunner
    {
        const int _maxProcessLifetime = 25000;
        const string _basePath = "/tmp";
        readonly string _tmpRuntimeConfigPath = Path.Combine(_basePath, "UserProgram.runtimeconfig.json");

        const string _runtimeConfig =
               """
                {
                    "runtimeOptions": {
                         "configProperties": {
                            "System.GC.Server": false
                    },
                    "tfm": "net10.0",
                    "framework": {
                        "name": "Microsoft.NETCore.App",
                        "version": "10.0.1"
                        }
                    }
                }
               """;

        static string[] _dllsToCopy = new[] {
            "nunitlite.dll",
            "nunit.framework.dll",
        };

        private static int _compilationCount = 0;

        private static List<AssemblyMetadata> _metadataCache;

        private bool _isDisposed;

        private ITestWrapper _codeWrapper;

        static readonly ProcessStartInfo _supervisorPsi = new ProcessStartInfo
        {
            FileName = "RunnerSupervisor",
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };


        static DotNetRunner()
        {
            _metadataCache = new List<AssemblyMetadata>();

            var systemAssemblies = new[]
            {
                typeof(object).Assembly.Location,
                typeof(Console).Assembly.Location,
                typeof(Enumerable).Assembly.Location,
                typeof(List<>).Assembly.Location,
                Assembly.Load("System.Runtime").Location,
                typeof(Task).Assembly.Location,
                typeof(Assert).Assembly.Location,
                typeof(AutoRun).Assembly.Location,
                typeof(JsonSerializer).Assembly.Location
            };

            foreach (var dll in systemAssemblies)
            {
                _metadataCache.Add(AssemblyMetadata.CreateFromFile(dll));
            }

            var appDlls = Directory.GetFiles("/app", "*.dll", new EnumerationOptions() { RecurseSubdirectories = true });

            foreach (var dll in appDlls)
            {
                try
                {
                    _metadataCache.Add(AssemblyMetadata.CreateFromFile(dll));

                    var dest = Path.Combine("/tmp", Path.GetFileName(dll));
                    File.Copy(dll, dest, overwrite: true);

                    Console.WriteLine($"Loaded & copied: {Path.GetFileName(dll)}");
                }
                catch
                {
                    Console.WriteLine($"Skipped: {Path.GetFileName(dll)}");
                }
            }

            var extraAssemblies = new[]
            {
                "System.Data.Common.dll",
                "System.Linq.Expressions.dll",
                "System.ComponentModel.TypeConverter.dll",
                "System.Collections.dll",
            };

            string runtimeDir = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
            foreach (var dllName in extraAssemblies)
            {
                var dllPath = Path.Combine(runtimeDir, dllName);
                if (File.Exists(dllPath))
                {
                    _metadataCache.Add(AssemblyMetadata.CreateFromFile(dllPath));
                }
            }
        }

        public DotNetRunner(ITestWrapper wrapper)
        {
            File.WriteAllText(_tmpRuntimeConfigPath, _runtimeConfig);

            _codeWrapper = wrapper;
        }

        public Task<CompilationResult> CompileCodeAsync(ProblemSolutionDto userSolution, CancellationToken cancellationToken = default)
        {
            ManifestDto manifest;
            try
            {
                manifest = ManifestParser.Parse(userSolution.TestManifestJson, userSolution.LanguageCode); // конвертация тестового манифеста из JSON в объектное представление
            }
            catch (InvalidTestTemplateException ex)
            {
                return Task.FromResult
                    (
                    new CompilationResult()
                    {
                        Success = false,
                        CompilationErrors = $"Manifest validation failed: {ex.Message}",
                        TotalTests = 0,
                        ExecutablePath = ""
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
                      CompilationErrors = $"Manifest JSON parse error: {ex.Message}",
                      TotalTests = 0,
                      ExecutablePath = ""
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
                        CompilationErrors = $"Manifest parse error: {ex.Message}",
                        TotalTests = 0,
                        ExecutablePath = ""
                    }
                    );
            }

            if (manifest.SampleTests.Count == 0 && !manifest.AdvancedTests.Any(t => t.LanguageCode == userSolution.LanguageCode))
                return Task.FromResult
                   (
                   new CompilationResult()
                   {
                       Success = false,
                       CompilationErrors = $"Invalid manifest: no tests for {userSolution.LanguageCode} detected",
                       TotalTests = 0,
                       ExecutablePath = ""
                   }
                   );

            var executablePath = Path.Combine(_basePath, $"{userSolution.RequestId:N}.dll"); // формирование пути будущего исполняемого файла

            var fullCode = _codeWrapper.GenerateSource(manifest, userSolution.UserSolution, "SolutionContainer"); // получение итогового текста программы

            var syntaxTree = CSharpSyntaxTree.ParseText(fullCode, cancellationToken: cancellationToken); // создание синтаксического дерева на основе полученного итогового текста программы

            var options = new CSharpCompilationOptions(
                OutputKind.ConsoleApplication,
                optimizationLevel: OptimizationLevel.Release,
                allowUnsafe: false); // составление опций компиляции (уровень оптимизации, тип выходного файла, ограничение на использование unsafe-кода)

            var compiledAssembly = CSharpCompilation.Create(
                "UserProgram",
                new[] { syntaxTree },
                GetReferences(),
                options); // компиляциия синт. дерева в сборку (Assembly)


            using var ms = new MemoryStream();

            var compilationResult = compiledAssembly.Emit(ms, cancellationToken: cancellationToken); // генерация IL-кода в указанный поток памяти

            var compilationResultString = string.Join("\n", compilationResult.Diagnostics); // составление результатов компиляции

            if (compilationResult.Success)
            {
                ms.Seek(0, SeekOrigin.Begin);
                using var fs = File.Create(executablePath); // запись скомпилированной программы на диск
                ms.CopyTo(fs);
            }

            if (Interlocked.Increment(ref _compilationCount) % 5 == 0) // выполнение сборки мусора каждые 5 компиляций
            {
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
                GC.WaitForPendingFinalizers();
            }

            return Task.FromResult
                (
                new CompilationResult()
                {
                    Success = compilationResult.Success,
                    CompilationErrors = compilationResultString,
                    TotalTests = manifest.SampleTests.Count + manifest.AdvancedTests.Count,
                    ExecutablePath = executablePath,
                });
        }

        public async Task<CodeResponseDto> ExecuteCodeAsync(ExecutionData data, CancellationToken cancellationToken = default)
        {
            var result = new CodeResponseDto() // предварительное формирование ответа
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

            var runRequest = new RunRequestDto() // формирование запроса к компоненту тестирования
            {
                ExecutorFileName = "dotnet",
                CommandLineArguments = ["exec", "--runtimeconfig", _tmpRuntimeConfigPath],
                ExecutableFileName = data.ExecutablePath,
                MaxProcessLifetime = _maxProcessLifetime
            };

            var serializedRequest = JsonSerializer.Serialize(runRequest); // сериализация запроса

            using var supervisorProc = new Process()
            {
                StartInfo = _supervisorPsi
            };

            supervisorProc.Start(); // запуск компонента тестирования

            await supervisorProc.StandardInput.WriteAsync(serializedRequest); // запись запроса в StdIn компонента тестирования

            supervisorProc.StandardInput.Close(); // закрытие StdIn

            var stdoutTask = supervisorProc.StandardOutput.ReadToEndAsync(cancellationToken); // получение задачи на чтение stdout
            var stderrTask = supervisorProc.StandardError.ReadToEndAsync(cancellationToken); // получение задачи на чтение stderr

            if (!supervisorProc.WaitForExit(_maxProcessLifetime))
            {
                supervisorProc.Kill();
                supervisorProc.WaitForExit();

                result.Status = RequestStatus.Failed;
                result.Result.Status = ExecutionStatus.TimedOut;
                result.Result.ExitCode = 124;
                result.Result.ConsoleOutput = "Supervisor timed out.";

                File.Delete(data.ExecutablePath);

                return result;
            }

            supervisorProc.WaitForExit();

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
                foreach (var md in _metadataCache)
                    md.Dispose();

                if (File.Exists(_tmpRuntimeConfigPath))
                    File.Delete(_tmpRuntimeConfigPath);

                foreach (var dll in _dllsToCopy)
                {
                    var dest = Path.Combine("/tmp", dll);
                    if (File.Exists(dest))
                        File.Delete(dest);
                }
            }
            _isDisposed = true;
        }

        private static IEnumerable<MetadataReference> GetReferences()
        {
            return _metadataCache.Select(am => am.GetReference());
        }
    }
}
