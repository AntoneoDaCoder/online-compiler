using Shared.DTOs;
using Shared.Helpers;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace RunnerSupervisor;

public class Program
{
    private static readonly JsonSerializerOptions _opt = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    static async Task<int> Main()
    {
        try
        {
            var input = await Console.In.ReadToEndAsync();
            var req = JsonSerializer.Deserialize<RunRequestDto>(input, _opt);

            if (req is null)
            {
                await WriteFailure("RunRequest was null");
                return 0;
            }

            var result = await ExecuteAsync(req);
            await Console.Out.WriteAsync(JsonSerializer.Serialize(result, _opt));
            return 0;
        }
        catch (Exception ex)
        {
            await WriteFailure("Caught exception in Main: " + ex.Message);
            return 0;
        }
    }

    private static async Task WriteFailure(string message)
    {
        var failure = new RunResultDto
        {
            TestReport = null,
            Status = Shared.Enums.ExecutionStatus.FailedToExecute,
            State = message,
            ExitCode = 1
        };

        await Console.Out.WriteAsync(JsonSerializer.Serialize(failure, _opt));
    }

    private static async Task<RunResultDto> ExecuteAsync(RunRequestDto req)
    {
        if (string.IsNullOrWhiteSpace(req.ExecutorFileName))
            return Fail("No executor was given");

        if (string.IsNullOrWhiteSpace(req.ExecutableFileName))
            return Fail("No executable was given");

        if (req.MaxProcessLifetime <= 0)
            req.MaxProcessLifetime = 25_000;

        var psi = new ProcessStartInfo
        {
            FileName = req.ExecutorFileName,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        foreach (var arg in req.CommandLineArguments ?? Enumerable.Empty<string>())
            psi.ArgumentList.Add(arg);

        psi.ArgumentList.Add(req.ExecutableFileName);

        try
        {
            using var proc = new Process { StartInfo = psi };

            if (!proc.Start())
                return Fail("Failed to start executable process");

            var monitor = new LinuxProcessTreeMonitor(proc.Id);
            var monitorTask = monitor.StartAsync();

            var sw = Stopwatch.StartNew();

            var stdoutTask = proc.StandardOutput.ReadToEndAsync();
            var stderrTask = proc.StandardError.ReadToEndAsync();

            var exited = proc.WaitForExit(req.MaxProcessLifetime);

            if (!exited)
            {
                TryKill(proc);
                proc.WaitForExit();

                sw.Stop();

                await Task.WhenAll(stdoutTask, stderrTask);
                await monitor.StopAsync();

                return new RunResultDto
                {
                    Status = Shared.Enums.ExecutionStatus.TimedOut,
                    State = "Code execution timed out.",
                    ExitCode = 124,
                    WallTimeMs = sw.ElapsedMilliseconds,
                    CpuTimeUs = monitor.CpuTimeUs,
                    PeakMemoryBytes = monitor.PeakMemoryBytes,
                    StdOut = stdoutTask.Result,
                    StdErr = stderrTask.Result
                };
            }

            proc.WaitForExit();

            await Task.WhenAll(stdoutTask, stderrTask);
            await monitor.StopAsync();

            sw.Stop();

            var stdout = stdoutTask.Result;
            var stderr = stderrTask.Result;

            var report = RunnerOutputParser.TryParseReport(stdout);

            return report is null
                ? new RunResultDto
                {
                    State = "Code didn't emit a proper report",
                    Status = Shared.Enums.ExecutionStatus.FailedToExecute,
                    StdErr = stderr,
                    StdOut = stdout,
                    ExitCode = proc.ExitCode,
                    WallTimeMs = sw.ElapsedMilliseconds,
                    CpuTimeUs = monitor.CpuTimeUs,
                    PeakMemoryBytes = monitor.PeakMemoryBytes
                }
                : new RunResultDto
                {
                    WallTimeMs = sw.ElapsedMilliseconds,
                    CpuTimeUs = monitor.CpuTimeUs,
                    PeakMemoryBytes = monitor.PeakMemoryBytes,
                    TestReport = report,
                    State = "Success",
                    Status = Shared.Enums.ExecutionStatus.Succeeded,
                    StdOut = stdout,
                    StdErr = stderr,
                    ExitCode = proc.ExitCode
                };
        }
        catch (Exception ex)
        {
            return Fail("Exception: " + ex.Message);
        }
    }

    private static RunResultDto Fail(string msg) => new()
    {
        Status = Shared.Enums.ExecutionStatus.FailedToExecute,
        State = msg,
        ExitCode = 1
    };

    private static void TryKill(Process proc)
    {
        try { proc.Kill(entireProcessTree: true); }
        catch { }
    }
}

internal sealed class LinuxProcessTreeMonitor
{
    private readonly int _rootPid;
    private readonly CancellationTokenSource _cts = new();
    private Task? _loopTask;

    private readonly HashSet<int> _tracked = new();
    private readonly Dictionary<int, long> _lastCpu = new();

    private long _completedCpu;
    private long _currentCpu;
    private long _peakRss;

    private readonly long _ticksPerSecond;

    public long CpuTimeUs =>
        _ticksPerSecond > 0
            ? (_completedCpu + _currentCpu) * 1_000_000L / _ticksPerSecond
            : 0;

    public long PeakMemoryBytes => _peakRss;

    public LinuxProcessTreeMonitor(int rootPid)
    {
        _rootPid = rootPid;
        _ticksPerSecond = sysconf(2); // _SC_CLK_TCK
    }

    public Task StartAsync()
    {
        _loopTask = Task.Run(Loop);
        return Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        _cts.Cancel();
        if (_loopTask != null)
        {
            try { await _loopTask; } catch { }
        }
    }

    private async Task Loop()
    {
        _tracked.Add(_rootPid);

        while (!_cts.IsCancellationRequested)
        {
            Sample();
            await Task.Delay(20, _cts.Token).ContinueWith(_ => { });
        }

        Sample();
    }

    private void Sample()
    {
        var snapshot = ReadProc();

        long cpu = 0;
        long rss = 0;

        foreach (var (pid, info) in snapshot)
        {
            if (pid == _rootPid || _tracked.Contains(info.Ppid))
                _tracked.Add(pid);
        }

        foreach (var pid in _tracked.ToArray())
        {
            if (snapshot.TryGetValue(pid, out var info))
            {
                cpu += info.Cpu;
                rss += info.Rss;
                _lastCpu[pid] = info.Cpu;
            }
            else
            {
                if (_lastCpu.TryGetValue(pid, out var last))
                    _completedCpu += last;

                _tracked.Remove(pid);
                _lastCpu.Remove(pid);
            }
        }

        _currentCpu = cpu;
        if (rss > _peakRss) _peakRss = rss;
    }

    private static Dictionary<int, ProcInfo> ReadProc()
    {
        var dict = new Dictionary<int, ProcInfo>();

        foreach (var dir in Directory.EnumerateDirectories("/proc"))
        {
            var name = Path.GetFileName(dir);
            if (!int.TryParse(name, out var pid)) continue;

            if (TryRead(pid, out var info))
                dict[pid] = info;
        }

        return dict;
    }

    private static bool TryRead(int pid, out ProcInfo info)
    {
        info = default;

        try
        {
            var text = File.ReadAllText($"/proc/{pid}/stat");
            var r = text.LastIndexOf(')');
            var parts = text[(r + 2)..].Split(' ');

            var ppid = int.Parse(parts[1]);
            var utime = long.Parse(parts[11]);
            var stime = long.Parse(parts[12]);
            var rssPages = long.Parse(parts[21]);

            var rss = rssPages > 0
                ? rssPages * Environment.SystemPageSize
                : 0;

            info = new ProcInfo(pid, ppid, utime + stime, rss);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private record struct ProcInfo(int Pid, int Ppid, long Cpu, long Rss);

    [DllImport("libc")]
    private static extern long sysconf(int name);
}