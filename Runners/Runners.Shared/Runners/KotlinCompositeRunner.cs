//using Runners.Shared;
//using Shared.DTOs;
//using System.Diagnostics;
//using System.Text.Json;
//using System.Text.Json.Serialization;

//public class KotlinCompositeRunner : IRunner
//{
//    private static readonly JsonSerializerOptions _options = new JsonSerializerOptions
//    {
//        PropertyNameCaseInsensitive = true,
//        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
//        Converters = { new JsonStringEnumConverter() }
//    };

//    private static readonly ProcessStartInfo _pInfo = new ProcessStartInfo()
//    {
//        FileName = "java",
//        Arguments = "-jar KotlinRunner/runner.jar --once",
//        RedirectStandardInput = true,
//        RedirectStandardOutput = true,
//        RedirectStandardError = true,
//        UseShellExecute = false,
//    };

//    private ProblemSolutionDto? _dto;
//    private bool _isDisposed;

//    public async Task<CodeResponseDto> ExecuteCodeAsync(Guid requestId, DateTime requestDate, CancellationToken cancellationToken)
//    {
//        using var proc = new Process() { StartInfo = _pInfo };
//        proc.Start();

//        var requestJson = JsonSerializer.Serialize(_dto, _options);
//        await proc.StandardInput.WriteLineAsync(requestJson);

//        proc.StandardInput.Close();

//        string output = await proc.StandardOutput.ReadToEndAsync();
//        string errors = await proc.StandardError.ReadToEndAsync();

//        await proc.WaitForExitAsync(cancellationToken);

//        if (string.IsNullOrWhiteSpace(output))
//        {
//            throw new InvalidOperationException($"Kotlin runner produced no output. Errors: {errors}");
//        }

//        try
//        {
//            return JsonSerializer.Deserialize<CodeResponseDto>(output, _options)!;
//        }
//        catch (Exception ex)
//        {
//            throw new InvalidOperationException(
//                $"Failed to parse Kotlin runner output. Raw output:\n{output}\nErrors:\n{errors}", ex);
//        }
//    }

//    public Task<(bool Success, string CompilationErrors)> CompileCodeAsync(string fullCode, CancellationToken cancellationToken)
//    {
//        if (_dto == null)
//            throw new InvalidOperationException("Compile error. Failed to compile code");

//        return Task.FromResult((true, string.Empty));
//    }

//    public string WrapCode(ProblemSolutionDto problemSolutionDto)
//    {
//        _dto = problemSolutionDto;

//        return JsonSerializer.Serialize(problemSolutionDto, _options);
//    }

//    public void Dispose()
//    {
//        if (_isDisposed) return;
//        _isDisposed = true;
//    }
//}
