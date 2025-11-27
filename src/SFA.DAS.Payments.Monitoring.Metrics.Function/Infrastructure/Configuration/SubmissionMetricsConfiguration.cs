namespace SFA.DAS.Payments.Monitoring.Metrics.Function.Infrastructure.Configuration
{
    public class SubmissionMetricsConfiguration : ISubmissionMetricsConfiguration
    {
        public string PaymentsConnectionString { get; set; }
        public int SqlMaxRetryCount { get; set; }
        public int SqlMaxRetryDelay { get; set; }
    }
}