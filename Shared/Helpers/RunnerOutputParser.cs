using Shared.DTOs;
using Shared.DTOs.TestReports;
using Shared.Enums;
using System.Text;
using System.Text.Json;

namespace Shared.Helpers
{
    public static class RunnerOutputParser
    {
        private const string _reportBeginMarker = "__TEST_REPORT_BEGIN__";
        private const string _reportEndMarker = "__TEST_REPORT_END__";

        private static readonly JsonSerializerOptions _opt = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public static TestRunReportDto? TryParseReport(string? stdout)
        {
            if (string.IsNullOrWhiteSpace(stdout))
                return null;

            var begin = stdout.IndexOf(_reportBeginMarker, StringComparison.Ordinal);
            if (begin < 0)
                return null;

            begin += _reportBeginMarker.Length;

            var end = stdout.IndexOf(_reportEndMarker, begin, StringComparison.Ordinal);
            if (end < 0 || end <= begin)
                return null;

            var json = stdout[begin..end].Trim();

            try
            {
                return JsonSerializer.Deserialize<TestRunReportDto>(json, _opt);
            }
            catch
            {
                return null;
            }
        }

        public static CodeResponseDto BuildExecutionReportOnProcOutput(CodeResponseDto result, RunResultDto report)
        {
            result.Result.ExitCode = report.ExitCode;

            result.Result.TotalTests = report?.TestReport?.TotalTests ?? 0;
            result.Result.PassedTests = report?.TestReport?.PassedTests ?? 0;

            result.Result.CpuTimeUs = report?.CpuTimeUs ?? 0;
            result.Result.WallTimeMs = report?.WallTimeMs ?? 0;
            result.Result.PeakMemoryBytes = report?.PeakMemoryBytes ?? 0;

            result.Result.Status = report!.Status;

            result.Result.ConsoleOutput = $"State: {report.State}\n" + BuildConsoleOutput(report!.TestReport, report.StdOut, report.StdErr);

            if (report!.TestReport != null)
            {
                if (report.TestReport.PassedTests == report.TestReport.TotalTests)
                {
                    result.Status = RequestStatus.Succeeded;
                }
                else
                {
                    result.Status = RequestStatus.Failed;
                }
            }
            else
            {
                if (report.Status == ExecutionStatus.Succeeded)
                {
                    result.Status = RequestStatus.Succeeded;
                }
                else
                {
                    result.Status = RequestStatus.Failed;
                }
            }

            return result;
        }

        private static string BuildConsoleOutput(TestRunReportDto? report, string? stdout, string? stderr)
        {
            if (report != null)
            {
                if (report.FailedTests == null || report.FailedTests.Length == 0)
                    return string.Empty;

                var sb = new StringBuilder();
                foreach (var f in report.FailedTests)
                {
                    sb.AppendLine($"{f.Name}: {f.Reason}");
                }
                return sb.ToString().Trim();
            }

            var combined = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(stdout))
            {
                combined.AppendLine("--- STDOUT ---");
                combined.AppendLine(stdout.Trim());
            }

            if (!string.IsNullOrWhiteSpace(stderr))
            {
                combined.AppendLine("--- STDERR ---");
                combined.AppendLine(stderr.Trim());
            }

            return combined.ToString().Trim();
        }
    }
}
