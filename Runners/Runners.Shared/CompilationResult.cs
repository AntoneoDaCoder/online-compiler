namespace Runners.Shared
{
    public class CompilationResult
    {
        public bool Success { get; set; }
        public string? CompilationErrors { get; set; }
        public int TotalTests { get; set; }
    }
}
