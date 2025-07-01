using System.Diagnostics;
using System.IO;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

class InMemoryRunner
{
    //const string _tmpDllPath = "/tmp/UserProgram.dll";

    public static async Task<int> Main()
    {
        string? code = Environment.GetEnvironmentVariable("USER_CODE");
        if (string.IsNullOrWhiteSpace(code))
        {
            Console.Error.WriteLine("No USER_CODE provided");
            return 1;
        }

        string boilerplateUsings = """
                using System;
                using System.Collections.Generic;
                using System.Linq;
                using System.Text;
                using System.Threading.Tasks;
                """;

        var fullCode = boilerplateUsings + "\n" + code;

        using var cts = new CancellationTokenSource();
        int timeoutInMilliseconds = int.TryParse(Environment.GetEnvironmentVariable("EXECUTION_TIMEOUT"), out var t) ? t : 3;

        cts.CancelAfter(TimeSpan.FromMilliseconds(timeoutInMilliseconds));


        var syntaxTree = CSharpSyntaxTree.ParseText(fullCode);

        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
            .Select(a => MetadataReference.CreateFromFile(a.Location));

        var options = new CSharpCompilationOptions(
             OutputKind.ConsoleApplication,
             optimizationLevel: OptimizationLevel.Release,
             allowUnsafe: false);

        var compiledAssembly = CSharpCompilation.Create(
            "UserProgram",
            new[] { syntaxTree },
            references,
            options);

        using var ms = new MemoryStream();
        var result = compiledAssembly.Emit(ms);

        if (!result.Success)
        {
            Console.WriteLine("[Compile error]");
            foreach (var diag in result.Diagnostics)
                Console.Error.WriteLine(diag.ToString());
            return 1;
        }

        ms.Seek(0, SeekOrigin.Begin);

        //File.WriteAllBytes(_tmpDllPath, ms.ToArray());

        //var proc = new Process
        //{
        //    StartInfo = new ProcessStartInfo
        //    {
        //        FileName = "dotnet",
        //        Arguments = $"{_tmpDllPath}",
        //        RedirectStandardOutput = true,
        //        RedirectStandardError = true
        //    }
        //};
        //proc.Start();

        //if (!proc.WaitForExit(timeoutInMilliseconds))
        //{
        //    proc.Kill();
        //    Console.Error.WriteLine("Execution timed out.");
        //    return 124;
        //}

        //Console.WriteLine(await proc.StandardOutput.ReadToEndAsync());
        //Console.Error.WriteLine(await proc.StandardError.ReadToEndAsync());

        //return proc.ExitCode;

        var assembly = Assembly.Load(ms.ToArray());

        var entryPoint = assembly.EntryPoint;
        Task task;

        if (entryPoint.GetParameters().Length == 0)
            task = Task.Run(() => entryPoint.Invoke(null, null));
        else
            task = Task.Run(() => entryPoint.Invoke(null, new object[] { Array.Empty<string>() }));

        await Task.WhenAny(task, Task.Delay(Timeout.Infinite, cts.Token));

        if (task.IsFaulted)
        {
            Console.WriteLine("[Runtime error]");
            var ex = task.Exception?.Flatten()?.InnerExceptions.FirstOrDefault();
            if (ex != null)
            {
                while (ex is TargetInvocationException tie && tie.InnerException != null)
                    ex = tie.InnerException;

                Console.Error.WriteLine($"{ex.GetType()}: {ex.Message}");

                var stack = ex.StackTrace?
                    .Split('\n')
                    .Where(line =>
                        !line.Contains("System.RuntimeMethodHandle") &&
                        !line.Contains("System.Reflection") &&
                        !line.Contains("System.Threading.Tasks") &&
                        !line.Contains("InMemoryRunner"))
                    .ToArray();

                if (stack != null)
                    foreach (var line in stack)
                        Console.Error.WriteLine(line.Trim());
            }

            return 1;
        }

        if (cts.IsCancellationRequested)
        {
            Console.Error.WriteLine("Execution timed out.");
            return 124;
        }

        return 0;
    }
}
