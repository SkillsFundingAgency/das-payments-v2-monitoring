using System;
using System.Collections.Generic;
using SFA.DAS.Payments.Application.Infrastructure.Telemetry;

namespace SFA.DAS.Payments.Monitoring.Metrics.Application.Submission
{
    public abstract class InstrumentedMetricsRepository
    {
        private readonly ITelemetry telemetry;
        public InstrumentedMetricsRepository(ITelemetry telemetry)
        {
            this.telemetry = telemetry ?? throw new ArgumentNullException(nameof(telemetry));
        }

        protected void SendMetricsTelemetry(string operation, long jobId, long ukprn, Guid correlationId, long elapsedMilliseconds)
        {
            var properties = new Dictionary<string, string>
            {
                { TelemetryKeys.JobId, jobId.ToString()},
                { TelemetryKeys.Ukprn, ukprn.ToString()},
            };

            var logMessage = $"Executed {operation} for UKPRN {ukprn} correlation ID {correlationId} in {elapsedMilliseconds} milliseconds";

            telemetry.TrackEvent(logMessage, properties, new Dictionary<string, double>());
        }
    }
}
