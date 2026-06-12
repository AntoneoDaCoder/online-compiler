namespace Runners.Shared
{
    public class ExecutionData
    {
        public Guid RequestId { get; set; }
        public Guid VersionId { get; set; }
        public Guid UserId { get; set; }
        public string UserSolution { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public DateTimeOffset RequestDate { get; set; }
        public int TotalTests { get; set; }
        public required string ExecutablePath { get; set; }
    }
}
