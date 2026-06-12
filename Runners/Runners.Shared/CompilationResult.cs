namespace Runners.Shared
{
    public class CompilationResult
    {
        public required bool Success { get; set; }
        public required string? CompilationErrors { get; set; }
        public required int TotalTests { get; set; }
        public required string ExecutablePath { get; set; }
    }
}
