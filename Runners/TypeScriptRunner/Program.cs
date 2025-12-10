using Runners.Shared;
using Runners.Shared.CodeWrappers.Typescript;
using Runners.Shared.Runners;

public class Runner
{
    public static async Task<int> Main()
    {
        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (s, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        using (var runner = new RunnerBase(new TypeScriptRunner(new TypescriptWrapper())))
        {
            await runner.ListenAsync(cts.Token);
        }

        return 0;
    }
}