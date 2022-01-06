﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SFA.DAS.Payments.Application.Infrastructure.Logging;
using SFA.DAS.Payments.Application.Infrastructure.Telemetry;
using SFA.DAS.Payments.Core;
using SFA.DAS.Payments.Monitoring.Jobs.Application.Infrastructure.Configuration;
using SFA.DAS.Payments.Monitoring.Jobs.Data;
using SFA.DAS.Payments.Monitoring.Jobs.Model;

namespace SFA.DAS.Payments.Monitoring.Jobs.Application.JobProcessing.PeriodEnd
{
    public interface IPeriodEndStartJobStatusService : IPeriodEndJobStatusService { }
    public class PeriodEndStartJobStatusService : PeriodEndJobStatusService, IPeriodEndStartJobStatusService
    {
        private readonly IJobsDataContext context;

        public PeriodEndStartJobStatusService(
            IJobStorageService jobStorageService,
            IPaymentLogger logger, 
            ITelemetry telemetry, 
            IJobStatusEventPublisher eventPublisher,
            IJobServiceConfiguration config, 
            IJobsDataContext context) 
            : base(jobStorageService, logger, telemetry, eventPublisher, config)
        {
            this.context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public override async Task<(bool IsComplete, JobStatus? OverriddenJobStatus, DateTimeOffset? completionTime)> PerformAdditionalJobChecks(JobModel job, CancellationToken cancellationToken)
        {
            var outstandingJobs = await context.GetOutstandingOrTimedOutJobs(job, cancellationToken);

            var timeoutsPresent = outstandingJobs.Any(x =>
                (x.JobStatus == JobStatus.TimedOut ||
                 x.JobStatus == JobStatus.DcTasksFailed) &&
                x.EndTime > job.StartTime);

            if (timeoutsPresent) //fail fast
            {
                return (true, JobStatus.CompletedWithErrors, outstandingJobs.Max(x => x.EndTime));
            }

            var processingJobsPresent = outstandingJobs.Where(x => x.JobStatus == JobStatus.InProgress || x.DcJobSucceeded == null).ToList();

            if (processingJobsPresent.Any())
            {
                SendTelemetry(job, processingJobsPresent);

                var completionTimesForInProgressJobs = await context.GetAverageJobCompletionTimesForInProgressJobs(processingJobsPresent.Select(p => p.Ukprn).ToList(), cancellationToken);

                return (false, null, null);
            }

            var jobsWithoutSubmissionSummariesPresent = context.DoSubmissionSummariesExistForJobs(outstandingJobs);

            if (jobsWithoutSubmissionSummariesPresent.Any()) 
            {
                SendTelemetry(job, null, jobsWithoutSubmissionSummariesPresent);
                return (false, null, null);
            }

            return (true, null, DateTimeOffset.UtcNow);
        }

        private void SendTelemetry(JobModel job, List<OutstandingJobResult> processingJobsPresent = null, List<long?> jobsWithoutSubmissionSummariesPresent = null)
        {
            var properties = new Dictionary<string, string>
            {
                { TelemetryKeys.JobId, job.DcJobId.Value.ToString()},
                { TelemetryKeys.JobType, job.JobType.ToString("G")},
                { TelemetryKeys.Ukprn, job.Ukprn?.ToString() ?? string.Empty},
                { TelemetryKeys.InternalJobId, job.DcJobId.ToString()},
                { TelemetryKeys.CollectionPeriod, job.CollectionPeriod.ToString()},
                { TelemetryKeys.AcademicYear, job.AcademicYear.ToString()},
                { TelemetryKeys.Status, job.Status.ToString("G")}
            };

            if (processingJobsPresent != null)
            {
                properties.Add("InProgressJobsCount", processingJobsPresent.Count.ToString());
                properties.Add("InProgressJobsList", string.Join(", ", processingJobsPresent.Select(j => j.ToJson())));
            }
            
            
            if (jobsWithoutSubmissionSummariesPresent != null)
            {
                properties.Add("jobsWithoutSubmissionSummariesCount", jobsWithoutSubmissionSummariesPresent.Count.ToString());
                properties.Add("jobsWithoutSubmissionSummaries", string.Join(", ", jobsWithoutSubmissionSummariesPresent.Select(j => j.ToJson())));
            }
            
            Telemetry.TrackEvent("PeriodEndStart Job Status Update", properties, new Dictionary<string, double>());
        }
    }
}
