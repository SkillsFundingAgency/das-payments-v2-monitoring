﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Amqp.Framing;
using SFA.DAS.Payments.Application.Infrastructure.Logging;
using SFA.DAS.Payments.Application.Infrastructure.Telemetry;
using SFA.DAS.Payments.Monitoring.Jobs.Application.Infrastructure.Configuration;
using SFA.DAS.Payments.Monitoring.Jobs.Model;

namespace SFA.DAS.Payments.Monitoring.Jobs.Application.JobProcessing
{
    public interface IJobStatusService
    {
        Task<bool> ManageStatus(long jobId, CancellationToken cancellationToken);
    }

    public abstract class JobStatusService : IJobStatusService
    {
        public IJobServiceConfiguration Config { get; }
        protected IJobStorageService JobStorageService { get; }
        protected IPaymentLogger Logger { get; }
        protected ITelemetry Telemetry { get; }
        protected IJobStatusEventPublisher EventPublisher { get; }

        protected JobStatusService(IJobStorageService jobStorageService, IPaymentLogger logger, ITelemetry telemetry, IJobStatusEventPublisher eventPublisher, IJobServiceConfiguration config)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            this.JobStorageService = jobStorageService ?? throw new ArgumentNullException(nameof(jobStorageService));
            this.Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.Telemetry = telemetry ?? throw new ArgumentNullException(nameof(telemetry));
            this.EventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
        }

        protected virtual Task<bool> CheckSavedJobStatus(JobModel job, CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }

        protected virtual async Task<bool> IsJobTimedOut(JobModel job, CancellationToken cancellationToken)
        {
            var timedOutTime = DateTimeOffset.UtcNow;

            if (job.Status != JobStatus.InProgress || job.StartTime.Add(job.JobType == JobType.PeriodEndRunJob ? Config.PeriodEndRunJobTimeout : Config.EarningsJobTimeout) >= timedOutTime)
                return false;

            var status = JobStatus.TimedOut;
            if (job.DcJobSucceeded.HasValue)
                status = job.DcJobSucceeded.Value ? JobStatus.CompletedWithErrors : JobStatus.DcTasksFailed;

            Logger.LogWarning($"Job {job.DcJobId} has timed out. {(status != JobStatus.TimedOut ? $"but because DcJobSucceeded is {job.DcJobSucceeded}, " : "")}Setting JobStatus as {status}");

            return await CompleteJob(job, status, timedOutTime, cancellationToken).ConfigureAwait(false);
        }

        public virtual async Task<bool> ManageStatus(long jobId, CancellationToken cancellationToken)
        {
            Logger.LogInfo($"Now determining if job {jobId} has finished. ");

            var job = await JobStorageService.GetJob(jobId, cancellationToken).ConfigureAwait(false);

            if (job != null)
            {
                if (await CheckSavedJobStatus(job, cancellationToken).ConfigureAwait(false))
                    return true;

                if (await IsJobTimedOut(job, cancellationToken).ConfigureAwait(false))
                    return true;
            }

            var additionalJobChecksResult = await PerformAdditionalJobChecks(job, cancellationToken).ConfigureAwait(false);
            if (!additionalJobChecksResult.IsComplete)
            {
                return false;
            }

            var inProgressMessages = await JobStorageService.GetInProgressMessages(jobId, cancellationToken).ConfigureAwait(false);
            var completedItems = await GetCompletedMessages(jobId, inProgressMessages, cancellationToken).ConfigureAwait(false);

            Logger.LogInfo($"ManageJobStatus JobId : {job.DcJobId}, JobType: {job.JobType} Inprogress count: {inProgressMessages.Count}, completed count: {completedItems.Count}.");

            if (!completedItems.Any())
            {
                Logger.LogVerbose($"ManageJobStatus JobId : {job.DcJobId}, JobType: {job.JobType} Inprogress count: {inProgressMessages.Count}, completed count: {completedItems.Count}. Found no completed messages.");
                return false;
            }

            cancellationToken.ThrowIfCancellationRequested();

            await ManageMessageStatus(jobId, completedItems, inProgressMessages, cancellationToken).ConfigureAwait(false);

            var currentJobStatus = await UpdateJobStatus(jobId, completedItems, cancellationToken).ConfigureAwait(false);

            if (!inProgressMessages.All(inProgress => completedItems.Any(item => item.MessageId == inProgress.MessageId)))
            {
                Telemetry.TrackEvent(
                    $"ManageJobStatus JobId : {job.DcJobId}, JobType: {job.JobType} Inprogress count: {inProgressMessages.Count}, completed count: {completedItems.Count}. Cannot set status for job.",
                    new Dictionary<string, string> { { "In-Progress-messageIds", string.Join(", ", inProgressMessages.Select(j => j.MessageId.ToString())) } },
                    new Dictionary<string, double> { { "Inprogress count", inProgressMessages.Count }, { "completed count", completedItems.Count } }
                );
                return false;
            }

            var jobStatus = additionalJobChecksResult.OverriddenJobStatus ?? (currentJobStatus.hasFailedMessages ? JobStatus.CompletedWithErrors : JobStatus.Completed);
            var endTime =
                (additionalJobChecksResult.completionTime.HasValue &&
                 additionalJobChecksResult.completionTime > currentJobStatus.endTime.Value)
                    ? additionalJobChecksResult.completionTime.Value
                    : currentJobStatus.endTime.Value;

            Telemetry.TrackEvent($"ManageJobStatus JobId : {job.DcJobId}, JobType: {job.JobType} jobStatus: {jobStatus}, endTime: {endTime}, Inprogress count: {inProgressMessages.Count}, completed count: {completedItems.Count}. Now Completing job.");

            return await CompleteJob(jobId, jobStatus, endTime, cancellationToken).ConfigureAwait(false);
        }

        public virtual Task<(bool IsComplete, JobStatus? OverriddenJobStatus, DateTimeOffset? completionTime)> PerformAdditionalJobChecks(JobModel job, CancellationToken cancellationToken)
        {
            return Task.FromResult((true, (JobStatus?)null, (DateTimeOffset?)null));
        }

        protected virtual async Task ManageMessageStatus(long jobId, List<CompletedMessage> completedMessages, List<InProgressMessage> inProgressMessages, CancellationToken cancellationToken)
        {
            await JobStorageService.RemoveInProgressMessages(jobId, completedMessages.Select(item => item.MessageId).ToList(), cancellationToken).ConfigureAwait(false);

            await JobStorageService.RemoveCompletedMessages(jobId, completedMessages.Select(item => item.MessageId).ToList(), cancellationToken).ConfigureAwait(false);
        }

        private async Task<bool> CompleteJob(long jobId, JobStatus status, DateTimeOffset endTime, CancellationToken cancellationToken)
        {
            var job = await JobStorageService.GetJob(jobId, cancellationToken).ConfigureAwait(false);
            if (job == null)
            {
                Logger.LogWarning($"Attempting to record completion status for job {jobId} but the job has not been persisted to database.");
                return false;
            }

            return await CompleteJob(job, status, endTime, cancellationToken).ConfigureAwait(false);
        }

        protected virtual async Task<bool> CompleteJob(JobModel job, JobStatus status, DateTimeOffset endTime, CancellationToken cancellationToken)
        {
            job.Status = status;
            job.EndTime = endTime;
            await JobStorageService.SaveJobStatus(job.DcJobId.Value, status, endTime, cancellationToken).ConfigureAwait(false);

            SendTelemetry(job);

            if (job.Status == JobStatus.DcTasksFailed || job.Status == JobStatus.TimedOut)
            {
                Logger.LogWarning($"Finished recording completion status of job. Job: {job.DcJobId}, status: {job.Status}, end time: {job.EndTime}");
            }
            else
            {
                Logger.LogInfo($"Finished recording completion status of job. Job: {job.DcJobId}, status: {job.Status}, end time: {job.EndTime}");
            }

            return true;
        }

        private async Task<List<CompletedMessage>> GetCompletedMessages(long jobId, List<InProgressMessage> inProgressMessages, CancellationToken cancellationToken)
        {
            var completedMessages = await JobStorageService.GetCompletedMessages(jobId, cancellationToken)
                .ConfigureAwait(false);

            var completedItems = completedMessages
                .Where(completedMessage => inProgressMessages.Any(inProgress => inProgress.MessageId == completedMessage.MessageId)).ToList();

            return completedItems;
        }

        protected async Task<(bool hasFailedMessages, DateTimeOffset? endTime)> UpdateJobStatus(long jobId, List<CompletedMessage> completedItems, CancellationToken cancellationToken)
        {
            var statusChanged = false;
            var currentJobStatus = await JobStorageService.GetJobStatus(jobId, cancellationToken).ConfigureAwait(false);
            var completedItemsEndTime = completedItems.Max(item => item.CompletedTime);
            if (!currentJobStatus.endTime.HasValue || completedItemsEndTime > currentJobStatus.endTime)
            {
                currentJobStatus.endTime = completedItemsEndTime;
                statusChanged = true;
            }

            if (currentJobStatus.hasFailedMessages || completedItems.Any(item => !item.Succeeded))
            {
                currentJobStatus.hasFailedMessages = true;
                statusChanged = true;
            }

            if (statusChanged)
            {
                Logger.LogInfo($"Detected change in job status for job: {jobId}. Has failed messages: {currentJobStatus.hasFailedMessages}, End time: {currentJobStatus.endTime}");
                await JobStorageService.StoreJobStatus(jobId, currentJobStatus.hasFailedMessages, currentJobStatus.endTime, cancellationToken)
                    .ConfigureAwait(false);
            }

            return currentJobStatus;
        }

        private void SendTelemetry(JobModel job)
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

            var metrics = new Dictionary<string, double>
            {
                { TelemetryKeys.Duration, (job.EndTime.Value - job.StartTime).TotalMilliseconds},
            };
            if (job.JobType == JobType.EarningsJob)
                metrics.Add("Learner Count", job.LearnerCount ?? 0);
            Telemetry.TrackEvent("Finished Job", properties, metrics);
        }
    }
}