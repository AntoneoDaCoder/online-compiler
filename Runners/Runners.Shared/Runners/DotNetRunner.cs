using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using NUnitLite;
using Shared.DTOs;
using Shared.Enums;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Runners.Shared.Runners
{
    public class DotNetRunner : IRunner
    {
        const int _maxProcessLifetime = 25000;
        const string _tmpDllPath = "/tmp/UserProgram.dll";
        const string _tmpRuntimeConfigPath = "/tmp/UserProgram.runtimeconfig.json";

        const string _boilerplateUsings = """
                using System;
                using System.Collections.Generic;
                using System.Linq;
                using System.Text;
                using System.Threading.Tasks;
                using NUnit.Framework;
                using NUnitLite;
                using Microsoft.EntityFrameworkCore;
                """;

        const string _runtimeConfig =
               """
                {
                    "runtimeOptions": {
                         "configProperties": {
                            "System.GC.Server": false
                    },
                    "tfm": "net9.0",
                    "framework": {
                        "name": "Microsoft.NETCore.App",
                        "version": "9.0.0"
                        }
                    }
                }
               """;

        static string[] _dllsToCopy = new[] {
            "nunitlite.dll",
            "nunit.framework.dll",
            "Microsoft.EntityFrameworkCore.dll",
        };
        static ProcessStartInfo _pInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"{_tmpDllPath}",
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        private static int _compilationCount = 0;

        private static List<AssemblyMetadata> _metadataCache;

        private bool _isDisposed;

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
                typeof(DbContext).Assembly.Location,
                typeof(DbContextOptionsBuilder).Assembly.Location,
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

        public DotNetRunner()
        {
            File.WriteAllText(_tmpRuntimeConfigPath, _runtimeConfig);
        }

        public string WrapCode(ProblemSolutionDto problemSolutionDto)
        {
            var sb = new StringBuilder(_boilerplateUsings);

            foreach (var definition in problemSolutionDto.Problem.AdditionalDefinitions)
            {
                sb.AppendLine(definition.Value);

                if (definition.Value.Contains("DbContext"))
                {
                    sb.AppendLine(@"    public static class TestInfrastructure
                                        {
                                            public static string Schema = ""linq_schema"";

                                            public static AppDbContext CreateContext()
                                            {
                                                var connectionString = ""Host=postgres.postgresql.svc.cluster.local;Port=5432;Database=postgresdb;Username=postgresadmin;Password=admin123"";

                                                var options = new DbContextOptionsBuilder<AppDbContext>()
                                                            .UseNpgsql(connectionString, o => o.MigrationsHistoryTable(""__EFMigrationsHistory"", Schema))
                                                            .Options;

                                                var context = new AppDbContext(options);

                                                context.Database.ExecuteSql($""CREATE SCHEMA IF NOT EXISTS \""{Schema}\"""");

                                                context.Database.EnsureDeleted();
                                                context.Database.EnsureCreated();

                                                return context;
                                            }
                                        }"
                    );
                }
            }


            sb.AppendLine(
                $$"""
            {{problemSolutionDto.Code}}
            public class Program
            {
                static int Main(string[] args)
                {
                    var argsWithNoResult = args.Concat(new[] { "--noresult" }).ToArray();
                    var result = new AutoRun().Execute(argsWithNoResult);
                    Console.Out.Flush();
                    return result;
                }
            }
            [TestFixture]
            public class GeneratedTests
            {      
            """);


            foreach (var testCase in problemSolutionDto.Problem.TestCases)
            {
                sb.AppendLine(
                    $$"""
                [Test]
                public void {{testCase.Name}}()
                {
                    {{testCase.TestInitialization}}

                    var testTask = Task.Run( ()=>
                    {
                        {{testCase.InputExpression}}
                        {{testCase.OutputExpression}}
                    });
                    
                    try
                    {
                        if (!testTask.Wait(TimeSpan.FromMilliseconds({{problemSolutionDto.MaxAllowedTimeInMilliseconds}})))
                        {
                            Assert.Fail("Test execution timed out");
                        }
                    }
                    catch(AggregateException ae)
                    {
                        throw ae.InnerException ?? ae;
                    }
                }
                """
                    );
            }
            sb.AppendLine("}");

            return sb.ToString();
        }

        public Task<(bool Success, string CompilationErrors)> CompileCodeAsync(string fullCode, CancellationToken cancellationToken)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(fullCode, cancellationToken: cancellationToken);

            var options = new CSharpCompilationOptions(
                OutputKind.ConsoleApplication,
                optimizationLevel: OptimizationLevel.Release,
                allowUnsafe: false);


            var compiledAssembly = CSharpCompilation.Create(
                "UserProgram",
                new[] { syntaxTree },
                GetReferences(),
                options);


            using var ms = new MemoryStream();

            var compilationResult = compiledAssembly.Emit(ms, cancellationToken: cancellationToken);

            var compilationResultString = string.Join("\n", compilationResult.Diagnostics);

            if (compilationResult.Success)
            {
                ms.Seek(0, SeekOrigin.Begin);
                using var fs = File.Create(_tmpDllPath);
                ms.CopyTo(fs);
            }

            if (Interlocked.Increment(ref _compilationCount) % 5 == 0)
            {
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
                GC.WaitForPendingFinalizers();
            }

            return Task.FromResult((compilationResult.Success, compilationResultString));
        }

        public async Task<CodeResponseDto> ExecuteCodeAsync(Guid requestId, DateTime requestDate, CancellationToken cancellationToken)
        {
            var result = new CodeResponseDto()
            {
                RequestId = requestId,
                Language = "csharp",
                Result = new ExecutionResultDto()
                {
                    RequestSentAt = requestDate,
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

                File.Delete(_tmpDllPath);

                return result;
            }

            result.Result.ExitCode = proc.ExitCode;

            if (proc.ExitCode != 0)
            {
                result.Status = RequestStatus.Failed;
                result.Result.Status = ExecutionStatus.RuntimeError;

                //because nuunitlite throws everything into stdout (even errors, it treats them as test result)
                string errorString = await proc.StandardOutput.ReadToEndAsync(cancellationToken);

                if (errorString.Contains("Test execution timed out"))
                {
                    result.Result.Status = ExecutionStatus.TimedOut;
                }
                else if (errorString.Contains("AssertionException") || errorString.Contains("Failed :", StringComparison.OrdinalIgnoreCase))
                {
                    result.Result.Status = ExecutionStatus.FailedToExecute;
                }
                else
                {
                    result.Result.Status = ExecutionStatus.RuntimeError;
                }

                var failedTestNames = new StringBuilder();

                var matchCollection = Regex.Matches(errorString, @"\d+\)\s+Failed\s+:\s+([\w\.]+)");

                foreach (Match match in matchCollection)
                {
                    failedTestNames.AppendLine(match.Groups[1].Value);
                }

                result.Result.ConsoleOutput = failedTestNames.ToString();
            }
            else
            {
                result.Status = RequestStatus.Succeeded;
                result.Result.Status = ExecutionStatus.Succeeded;
            }

            File.Delete(_tmpDllPath);

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
