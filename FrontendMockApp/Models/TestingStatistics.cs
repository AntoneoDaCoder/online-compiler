namespace FrontendMockApp.Models
{
    public class TestingStatistics
    {
        public Guid SessionId { get; set; } = Guid.NewGuid();
        public string CaseDescription { get; set; } = string.Empty;
        public DateTime SessionStartedAt { get; set; } = DateTime.UtcNow;
        public DateTime SessionEndedAt { get; set; }
        public TimeSpan SessionDuration => SessionEndedAt - SessionStartedAt;
        public int RequestsSent { get; set; }
        public int ResponsesReceived { get; set; }
        public int ResponsesWithStatusCode { get; set; }
        public double HealthRatio => (double)ResponsesWithStatusCode / RequestsSent;
        public double AverageLatency { get; set; }
    }
}
