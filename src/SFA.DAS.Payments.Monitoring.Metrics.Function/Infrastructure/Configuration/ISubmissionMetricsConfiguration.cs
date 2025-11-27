namespace SFA.DAS.Payments.Monitoring.Metrics.Function.Infrastructure.Configuration
{
    public interface ISubmissionMetricsConfiguration
    {
        string PaymentsConnectionString { get; set; }
        int SqlMaxRetryCount { get; set; }
        int SqlMaxRetryDelay { get; set; }
    }
}