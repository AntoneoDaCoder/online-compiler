using Runners.Shared;
using Runners.Shared.Runners;
using Runners.Shared.Runners.Sql;

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

        using (var runner = new RunnerBase(new SqlRunner()))
        {
            await runner.ListenAsync(cts.Token);
        }

        return 0;
    }
}