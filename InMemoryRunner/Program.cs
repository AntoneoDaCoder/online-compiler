using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;
using NUnitLite;
using Shared.DTOs;
using Shared.Enums;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Text;
using System.Text.Json;

class Runner
{
    const string _tmpDllPath = "/tmp/UserProgram.dll";
    const string _tmpRuntimeConfigPath = "/tmp/UserProgram.runtimeconfig.json";
    const string _apiCallbackUrl = "http://api-server:8080/api/jobs/complete";
    const int _maxProcessLifetime = 25000;
    const string _boilerplateUsings = """
                using System;
                using System.Collections.Generic;
                using System.Linq;
                using System.Text;
                using System.Threading.Tasks;
                using NUnit.Framework;
                using NUnitLite;
                """;

    const string _runtimeConfig = """
                {
                    "runtimeOptions": {
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
            "nunit.framework.dll"
        };

    private static HttpListener _listener = new HttpListener();
    private static HttpClient _client = new HttpClient();
    private static List<AssemblyMetadata> _metadataCache;
    private static JsonSerializerOptions _options = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    };

    static Runner()
    {
        _metadataCache = new List<AssemblyMetadata>
        {
            AssemblyMetadata.CreateFromFile(typeof(object).Assembly.Location),
            AssemblyMetadata.CreateFromFile(typeof(Console).Assembly.Location),
            AssemblyMetadata.CreateFromFile(typeof(Enumerable).Assembly.Location),
            AssemblyMetadata.CreateFromFile(typeof(List<>).Assembly.Location),
            AssemblyMetadata.CreateFromFile(Assembly.Load("System.Runtime").Location),
            AssemblyMetadata.CreateFromFile(typeof(Task).Assembly.Location),
            AssemblyMetadata.CreateFromFile(typeof(Assert).Assembly.Location),
            AssemblyMetadata.CreateFromFile(typeof(AutoRun).Assembly.Location)
        };
    }

    public static async Task<int> Main()
    {
        AppDomain.CurrentDomain.ProcessExit += (_, __) => ReleaseResources();

        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (s, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        _listener.Prefixes.Add("http://*:5000/run/");
        _listener.Start();

        File.WriteAllText(_tmpRuntimeConfigPath, _runtimeConfig);



        foreach (var dll in _dllsToCopy)
        {
            var source = Path.Combine("/app", dll);
            var dest = Path.Combine("/tmp", dll);
            if (File.Exists(source))
                File.Copy(source, dest, overwrite: true);
        }

        await ListenAsync(cts.Token);

        File.Delete(_tmpRuntimeConfigPath);


        foreach (var dll in _dllsToCopy)
        {
            var dest = Path.Combine("/tmp", dll);
            if (File.Exists(dest))
                File.Delete(dest);
        }

        return 0;
    }

    private static IEnumerable<MetadataReference> GetReferences()
    {
        return _metadataCache.Select(am => am.GetReference());
    }

    private static async Task NotifyJobManagerAsync(CodeResponseDto response, string callbackUrl, Guid requestId, CancellationToken cancellationToken)
    {
        response.Result.ResponseSentAt = DateTime.UtcNow;

        await _client.PostAsJsonAsync(callbackUrl, response, cancellationToken);

        Console.WriteLine($"[Runner] Sent response [Id:{response.RequestId}] to request [Id:{requestId}]");
    }

    private static async Task ListenAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var context = await _listener.GetContextAsync();

                using (var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding))
                {
                    try
                    {
                        var requestString = await reader.ReadToEndAsync(cancellationToken);

                        var codeRequest = JsonSerializer.Deserialize<ProblemSolutionDto>(requestString, _options);

                        Console.WriteLine($"[Runner] Received request [Id:{codeRequest.RequestId}]");

                        _ = ExecuteUserCodeAsync(codeRequest, cancellationToken);

                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing callback: {ex}");
                    }
                    finally
                    {
                        context.Response.Close();
                    }
                }
            }
        }
        catch (HttpListenerException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Runner] Error in listening loop: {ex}");
        }
    }

    private static async Task ExecuteUserCodeAsync(ProblemSolutionDto request, CancellationToken cancellationToken)
    {
        var result = new CodeResponseDto()
        {
            RequestId = request.RequestId,
            Result = new ExecutionResultDto()
            {
                RequestSentAt = request.SentAt
            }
        };

        var fullCode = WrapUserCode(request);

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

        if (!compilationResult.Success)
        {
            result.Status = RequestStatus.Failed;
            result.Result.Status = ExecutionStatus.CompileError;
            result.Result.ExitCode = 1;

            foreach (var diag in compilationResult.Diagnostics)
                result.Result.ConsoleOutput = string.Join("\n", compilationResult.Diagnostics);

            await NotifyJobManagerAsync(result, _apiCallbackUrl, request.RequestId, cancellationToken);

            return;
        }

        ms.Seek(0, SeekOrigin.Begin);

        File.WriteAllBytes(_tmpDllPath, ms.ToArray());

        using var proc = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"{_tmpDllPath}",
                RedirectStandardOutput = true,
                RedirectStandardError = true
            }
        };
        proc.Start();

        if (!proc.WaitForExit(_maxProcessLifetime))
        {
            proc.Kill();
            result.Status = RequestStatus.Failed;
            result.Result.Status = ExecutionStatus.TimedOut;
            result.Result.ExitCode = 124;
            result.Result.ConsoleOutput = "Execution timed out.";

            await NotifyJobManagerAsync(result, _apiCallbackUrl, request.RequestId, cancellationToken);

            File.Delete(_tmpDllPath);

            return;
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
            else if (errorString.Contains("AssertionException"))
            {
                result.Result.Status = ExecutionStatus.FailedToExecute;
            }
            else
            {
                result.Result.Status = ExecutionStatus.RuntimeError;
            }

            result.Result.ConsoleOutput = errorString;
        }
        else
        {
            result.Status = RequestStatus.Succeeded;
            result.Result.Status = ExecutionStatus.Succeded;
        }

        File.Delete(_tmpDllPath);

        await NotifyJobManagerAsync(result, _apiCallbackUrl, request.RequestId, cancellationToken);
    }

    private static void ReleaseResources()
    {
        foreach (var md in _metadataCache)
            md.Dispose();

        _client.Dispose();
        _listener.Close();

        if (File.Exists(_tmpRuntimeConfigPath))
            File.Delete(_tmpRuntimeConfigPath);

        foreach (var dll in _dllsToCopy)
        {
            var dest = Path.Combine("/tmp", dll);
            if (File.Exists(dest))
                File.Delete(dest);
        }
    }

    private static string WrapUserCode(ProblemSolutionDto problemSolutionDto)
    {
        var sb = new StringBuilder(_boilerplateUsings);

        sb.AppendLine(
            $$"""
            {{problemSolutionDto.Problem.AdditionalDefinitions}}
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
}
