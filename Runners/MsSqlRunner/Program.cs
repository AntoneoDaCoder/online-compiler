using Runners.Shared;
using Runners.Shared.Runners;

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

        using (var runner = new RunnerBase(new MsSqlRunner()))
        {
            await runner.ListenAsync(cts.Token);
        }

        return 0;
    }
}
