using Runners.Shared.CodeWrappers.NodeJs;
using Runners.Shared.Runners;
using Runners.Shared;

class Runner
{
    public static async Task<int> Main()
    {
        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (s, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        using (var runner = new RunnerBase(new NodeJsRunner(new NodeJsWrapper())))
        {
            await runner.ListenAsync(cts.Token);
        }

        return 0;
    }
}