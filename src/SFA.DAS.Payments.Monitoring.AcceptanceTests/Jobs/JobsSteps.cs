﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Microsoft.EntityFrameworkCore;
using SFA.DAS.Payments.AcceptanceTests.Core;
using SFA.DAS.Payments.Core;
using SFA.DAS.Payments.Monitoring.Jobs.Messages.Commands;
using TechTalk.SpecFlow;
using NServiceBus;
using NUnit.Framework;
using SFA.DAS.Payments.AcceptanceTests.Core.Infrastructure;
using SFA.DAS.Payments.Monitoring.AcceptanceTests.Handlers;
using SFA.DAS.Payments.Monitoring.Jobs.Data;
using SFA.DAS.Payments.Monitoring.Jobs.Model;

namespace SFA.DAS.Payments.Monitoring.AcceptanceTests.Jobs
{
    [Binding]
    public class JobsSteps : StepsBase
    {
        private const string ProcessLearnerCommandNs = "SFA.DAS.Payments.EarningEvents.Commands.Internal.ProcessLearnerCommand";
        private const string PeriodEndLargeSubmissionJobIdKey = "period_end_large_submission_job_id";
        private const string JobDetailsKey = "job_command";

        protected JobsDataContext DataContext;

        protected List<long?> jobIdToBeDeleted = new List<long?>();

        protected JobModel Job
        {
            get => Get<JobModel>();
            set => Set(value);
        }
        public List<GeneratedMessage> GeneratedMessages
        {
            get => Get<List<GeneratedMessage>>();
            set => Set(value);
        }

        public long PeriodEndLargeSubmissionJobId
        {
            get => Get<long>(PeriodEndLargeSubmissionJobIdKey);
            set => Set(value, PeriodEndLargeSubmissionJobIdKey);
        }

        public JobsCommand JobDetails
        {
            get => Get<JobsCommand>(JobDetailsKey);
            set => Set(value, JobDetailsKey);
        }

        public JobsSteps(ScenarioContext context) : base(context)
        {
            var testConfiguration = Scope.Resolve<TestsConfiguration>();
            DataContext = new JobsDataContext(testConfiguration.PaymentsConnectionString);
        }

        protected string PartitionEndpointName => $"sfa-das-payments-monitoring-jobs{JobDetails.JobId % 2}";

        [AfterScenario]
        public async Task ClearTestData()
        {
            await ClearTestJobs();
            ClearTestSubmissionMetrics();
        }

        private async Task ClearTestJobs()
        {
            DataContext.Jobs.Remove(Job);

            if (jobIdToBeDeleted.Any())
            {
                var submissionJob = await DataContext.Jobs.Where(x => jobIdToBeDeleted.Contains(x.DcJobId)).ToListAsync();
                if (submissionJob.Any())
                    DataContext.Jobs.RemoveRange(submissionJob);

            }

            if (ScenarioContext.Current.ContainsKey(PeriodEndLargeSubmissionJobIdKey))
            {
                var submissionJob = DataContext.Jobs.FirstOrDefault(x => x.DcJobId == PeriodEndLargeSubmissionJobId);
                if (submissionJob != null)
                    DataContext.Jobs.Remove(submissionJob);
            }

            await DataContext.SaveChangesAsync();
        }

        private void ClearTestSubmissionMetrics()
        {
            DataContext.Database.ExecuteSqlCommand(
                $"DELETE FROM [Metrics].[SubmissionSummary] WHERE [AcademicYear] = 1819 AND [CollectionPeriod] = {CollectionPeriod}");
        }

        [Given(@"the payments are for the current collection year")]
        public void GivenThePaymentsAreForTheCurrentCollectionYear()
        {
            SetCurrentCollectionYear();
        }

        [Given(@"the current collection period is R(.*)")]
        [Given(@"the current processing period is (.*)")]
        public void GivenTheCurrentProcessingPeriodIs(byte period)
        {
            CollectionPeriod = period;
        }

        [Given(@"the period end service has received a period end job")]
        public void GivenThePeriodEndServiceHasReceivedAPeriodEndJob()
        {
            GeneratedMessages = new List<GeneratedMessage>
            {
                new GeneratedMessage
                {
                    StartTime = DateTimeOffset.UtcNow,
                    MessageName = "SFA.DAS.Payments.PeriodEnd.Messages.Events.PeriodEndStartedEvent",
                    MessageId = Guid.NewGuid()
                },
            };
            JobDetails = new RecordPeriodEndStartJob
            {
                JobId = TestSession.JobId,
                CollectionPeriod = CollectionPeriod,
                CollectionYear = 1819,
                StartTime = DateTimeOffset.UtcNow,
                GeneratedMessages = GeneratedMessages,
            };

            Console.WriteLine($"Job details: {JobDetails.ToJson()}");
        }

        [Given(@"the earnings event service has received a provider earnings job")]
        public void GivenTheEarningsEventServiceHasReceivedAProviderEarningsJob()
        {
            GeneratedMessages = new List<GeneratedMessage>
            {
                new GeneratedMessage
                {
                    StartTime = DateTimeOffset.UtcNow,
                    MessageName = ProcessLearnerCommandNs,
                    MessageId = Guid.NewGuid()
                },
                new GeneratedMessage
                {
                    StartTime = DateTimeOffset.UtcNow,
                    MessageName = ProcessLearnerCommandNs,
                    MessageId = Guid.NewGuid()
                },
                new GeneratedMessage
                {
                    StartTime = DateTimeOffset.UtcNow,
                    MessageName = ProcessLearnerCommandNs,
                    MessageId = Guid.NewGuid()
                },
            };
            JobDetails = new RecordEarningsJob
            {
                JobId = TestSession.JobId,
                CollectionPeriod = CollectionPeriod,
                CollectionYear = 1819,
                Ukprn = TestSession.Ukprn,
                StartTime = DateTimeOffset.UtcNow,
                IlrSubmissionTime = DateTime.UtcNow.AddSeconds(-10),
                GeneratedMessages = GeneratedMessages,
            };

            Console.WriteLine($"Job details: {JobDetails.ToJson()}");
        }

        [Given(@"the earnings event service has received a large provider earnings job")]
        public void GivenTheEarningsEventServiceHasReceivedALargeProviderEarningsJob()
        {
            GeneratedMessages = new List<GeneratedMessage>();
            for (var i = 0; i < 1500; i++)
            {
                GeneratedMessages.Add(new GeneratedMessage
                {
                    StartTime = DateTimeOffset.UtcNow,
                    MessageName = "SFA.DAS.Payments.PeriodEnd.Messages.Events.PeriodEndStartedEvent",
                    MessageId = Guid.NewGuid()
                });
            }

            JobDetails = new RecordEarningsJob
            {
                JobId = TestSession.JobId,
                CollectionPeriod = CollectionPeriod,
                CollectionYear = 1819,
                Ukprn = TestSession.Ukprn,
                StartTime = DateTimeOffset.UtcNow,
                IlrSubmissionTime = DateTime.UtcNow.AddSeconds(-10),
                GeneratedMessages = GeneratedMessages,
            };
            Console.WriteLine($"Job details: {JobDetails.ToJson()}");
        }

        [Given(@"a provider earnings job has already been recorded")]
        public async Task GivenAProviderEarningsJobHasAlreadyBeenRecorded()
        {
            GeneratedMessages = new List<GeneratedMessage>
            {
                new GeneratedMessage
                {
                    StartTime = DateTimeOffset.UtcNow,
                    MessageName = ProcessLearnerCommandNs,
                    MessageId = Guid.NewGuid()
                },
                new GeneratedMessage
                {
                    StartTime = DateTimeOffset.UtcNow,
                    MessageName = ProcessLearnerCommandNs,
                    MessageId = Guid.NewGuid()
                },
                new GeneratedMessage
                {
                    StartTime = DateTimeOffset.UtcNow,
                    MessageName = ProcessLearnerCommandNs,
                    MessageId = Guid.NewGuid()
                },
            };

            var earningsJob = new RecordEarningsJob
            {
                JobId = TestSession.JobId,
                CollectionPeriod = CollectionPeriod,
                CollectionYear = 1819,
                Ukprn = TestSession.Ukprn,
                StartTime = DateTimeOffset.UtcNow,
                IlrSubmissionTime = DateTime.UtcNow.AddSeconds(-10),
                GeneratedMessages = GeneratedMessages
            };
            JobDetails = earningsJob;

            Job = new JobModel
            {
                JobType = JobType.EarningsJob,
                StartTime = earningsJob.StartTime,
                CollectionPeriod = earningsJob.CollectionPeriod,
                AcademicYear = earningsJob.CollectionYear,
                Ukprn = earningsJob.Ukprn,
                DcJobId = JobDetails.JobId,
                IlrSubmissionTime = earningsJob.IlrSubmissionTime,
                Status = JobStatus.InProgress,
                LearnerCount = GeneratedMessages.Count
            };
            DataContext.Jobs.Add(Job);
            await DataContext.SaveChangesAsync();
            DataContext.JobSteps.AddRange(
                GeneratedMessages.Select(msg =>
                    new JobStepModel
                    {
                        JobId = Job.Id,
                        StartTime = msg.StartTime,
                        MessageName = msg.MessageName,
                        MessageId = msg.MessageId,
                        Status = JobStepStatus.Queued
                    }));
            await DataContext.SaveChangesAsync();
        }

        [Given(@"the period end service has received a period end start job")]
        public void GivenThePeriodEndServiceHasReceivedAPeriodEndStartJob()
        {
            CreatePeriodEndJob<RecordPeriodEndStartJob>();
        }

        private void CreatePeriodEndJob<T>() where T : RecordPeriodEndJob, new()
        {
            GeneratedMessages = new List<GeneratedMessage>
            {
                new GeneratedMessage
                {
                    StartTime = DateTimeOffset.UtcNow, MessageName = typeof(RecordPeriodEndStartJob).FullName,
                    MessageId = Guid.NewGuid()
                },
            };
            JobDetails = new T
            {
                JobId = TestSession.JobId,
                CollectionPeriod = CollectionPeriod,
                CollectionYear = 1819,
                StartTime = DateTimeOffset.UtcNow,
                GeneratedMessages = GeneratedMessages,
            };
            Console.WriteLine($"Job id: {TestSession.JobId}");

        }

        [Given(@"the period end service has received a period end run job")]
        public void GivenThePeriodEndServiceHasReceivedAPeriodEndRunJob()
        {
            CreatePeriodEndJob<RecordPeriodEndRunJob>();
        }

        [Given(@"the period end service has received a period end stop job")]
        public void GivenThePeriodEndServiceHasReceivedAPeriodEndStopJob()
        {
            CreatePeriodEndJob<RecordPeriodEndStopJob>();
        }

        [Given(@"the period end service has received a Validate Submission Window Job")]
        public void GivenThePeriodEndServiceHasReceivedAValidateSubmissionWindowJob()
        {
            CreatePeriodEndJob<RecordPeriodEndSubmissionWindowValidationJob>();
        }

        [Given(@"the monitoring service has recorded the completion of a period end start job")]
        public async Task GivenTheMonitoringServiceHasRecordedTheCompletionOfAPeriodEndStartJob()
        {
            GivenThePeriodEndServiceHasReceivedAPeriodEndJob();
            await WhenThePeriodEndServiceNotifiesTheJobMonitoringServiceToRecordTheJob().ConfigureAwait(false);
            await WhenTheFinalMessagesForTheJobAreSuccessfullyProcessed().ConfigureAwait(false);
            await ThenTheJobMonitoringServiceShouldRecordTheJob().ConfigureAwait(false);
        }

        [Given(@"the monitoring service has recorded the completion of an earnings job")]
        public async Task GivenTheMonitoringServiceHasRecordedTheCompletionOfAnEarningsJob()
        {
            GivenTheEarningsEventServiceHasReceivedAProviderEarningsJob();
            await WhenTheEarningsEventServiceNotifiesTheJobMonitoringServiceToRecordTheJob().ConfigureAwait(false);
            await WhenTheFinalMessagesForTheJobAreSuccessfullyProcessed().ConfigureAwait(false);
            await ThenTheJobMonitoringServiceShouldRecordTheJob().ConfigureAwait(false);
        }

        [When("the submission summary metrics are recorded")]
        public async Task WhenTheSubmissionSummaryMetricsAreRecorded()
        {
            var jobId = ScenarioContext.Current.ContainsKey(PeriodEndLargeSubmissionJobIdKey) ? PeriodEndLargeSubmissionJobId : JobDetails.JobId;

            await DataContext.Database.ExecuteSqlCommandAsync($@"INSERT INTO [Metrics].[SubmissionSummary]
                   ([Ukprn]
                   ,[AcademicYear]
                   ,[CollectionPeriod]
                   ,[JobId]
                   ,[Percentage]
                   ,[ContractType1]
                   ,[ContractType2]
                   ,[DifferenceContractType1]
                   ,[DifferenceContractType2]
                   ,[PercentageContractType1]
                   ,[PercentageContractType2]
                   ,[EarningsDCContractType1]
                   ,[EarningsDCContractType2]
                   ,[EarningsDASContractType1]
                   ,[EarningsDASContractType2]
                   ,[EarningsDifferenceContractType1]
                   ,[EarningsDifferenceContractType2]
                   ,[EarningsPercentageContractType1]
                   ,[EarningsPercentageContractType2]
                   ,[RequiredPaymentsContractType1]
                   ,[RequiredPaymentsContractType2]
                   ,[AdjustedDataLockedEarnings]
                   ,[AlreadyPaidDataLockedEarnings]
                   ,[TotalDataLockedEarnings]
                   ,[HeldBackCompletionPaymentsContractType1]
                   ,[HeldBackCompletionPaymentsContractType2]
                   ,[PaymentsYearToDateContractType1]
                   ,[PaymentsYearToDateContractType2]
                   ,[CreationDate])
             VALUES
                   ({TestSession.Ukprn}
                   ,1819
                   ,{CollectionPeriod}
                   ,{jobId}
                   ,100
                   ,100
                   ,100
                   ,0
                   ,0
                   ,100
                   ,100
                   ,100
                   ,100
                   ,100
                   ,100
                   ,100
                   ,100
                   ,100
                   ,100
                   ,100
                   ,100
                   ,100
                   ,100
                   ,100
                   ,100
                   ,100
                   ,100
                   ,100
                   ,CURRENT_TIMESTAMP)");
        }

        [When(@"the final messages for the job are successfully processed")]
        [When(@"the final messages for the job are successfully processed for the Period End Start job")]
        public async Task WhenTheFinalMessagesForTheJobAreSuccessfullyProcessed()
        {
            foreach (var generatedMessage in GeneratedMessages)
            {
                var message = new RecordJobMessageProcessingStatus
                {
                    JobId = JobDetails.JobId,
                    MessageName = generatedMessage.MessageName,
                    EndTime = DateTimeOffset.UtcNow,
                    Succeeded = true,
                    Id = generatedMessage.MessageId
                };
                Console.WriteLine($"Generated Message: {message.ToJson()}");
                await MessageSession.Send(PartitionEndpointName, message).ConfigureAwait(false);
            }
        }

        [Given(@"the earnings event service has received and is processing a provider earnings job")]
        public async Task GivenTheEarningsEventServiceHasReceivedAndIsProcessingAProviderEarningsJob()
        {
            PeriodEndLargeSubmissionJobId = TestSession.GenerateId();

            var jobModel = new JobModel
            {
                Ukprn = TestSession.Ukprn,
                CollectionPeriod = CollectionPeriod,
                AcademicYear = 1819,
                DcJobId = PeriodEndLargeSubmissionJobId,
                JobType = JobType.EarningsJob,
                StartTime = DateTimeOffset.UtcNow,
                IlrSubmissionTime = DateTime.UtcNow.AddSeconds(-10),
                Status = JobStatus.InProgress
            };
            await DataContext.SaveNewJob(jobModel);
        }

        [Given(@"the earnings event service has received and successfully processed a provider earnings job")]
        [Given(@"the earnings event service has received and successfully processed a (.*) provider earnings job")]
        public async Task GivenTheEarningsEventServiceHasReceivedAndSuccessfullyProcessedAProviderEarningsJob(string jobSize)
        {
            var jobId = TestSession.GenerateId();

            var largeJob = string.Equals(jobSize, "small", StringComparison.InvariantCultureIgnoreCase);
            var startTime = largeJob ? DateTimeOffset.UtcNow.AddSeconds(-10) : DateTimeOffset.UtcNow.AddSeconds(-60);
            var endTime = DateTimeOffset.UtcNow.AddSeconds(-10);

            jobIdToBeDeleted.Add(jobId);

            var jobModel = new JobModel
            {
                Ukprn = TestSession.Ukprn,
                CollectionPeriod = CollectionPeriod,
                AcademicYear = 1819,
                DcJobId = jobId,
                JobType = JobType.EarningsJob,
                StartTime = startTime,
                EndTime = endTime,
                IlrSubmissionTime = startTime.DateTime,
                Status = JobStatus.Completed,
                DcJobSucceeded = true,
                DcJobEndTime = endTime.DateTime
            };

            await DataContext.SaveNewJob(jobModel);
        }


        [When(@"the period end service notifies the job monitoring service to record the Validate Submission Window job")]
        public async Task WhenThePeriodEndServiceNotifiesTheJobMonitoringServiceToRecordTheValidateSubmissionWindowJob()
        {
            await NotifyRecordJob<RecordPeriodEndSubmissionWindowValidationJob>();
        }

        [Then(@"the period end job should not complete")]
        public async Task ButThePeriodEndJobDoesNotComplete()
        {
            await Task.Delay(TimeSpan.FromSeconds(10));

            if (DataContext.Jobs.AsNoTracking()
                .Any(x =>
                    x.DcJobId == JobDetails.JobId && x.Status == JobStatus.Completed && x.EndTime != null))
            {
                Assert.Fail($"Period End Start Job finished before expected. Id: {TestSession.JobId}");
            }
        }

        [When(@"the earnings event service notifies the job monitoring service to record the job")]
        public async Task WhenTheEarningsEventServiceNotifiesTheJobMonitoringServiceToRecordTheJob()
        {
            var recordEarningsJob = JobDetails as RecordEarningsJob;
            if (recordEarningsJob != null) recordEarningsJob.GeneratedMessages = GeneratedMessages.Take(1000).ToList();
            await MessageSession.Send(PartitionEndpointName, JobDetails).ConfigureAwait(false);
            var skip = 1000;
            var batch = new List<GeneratedMessage>();
            while ((batch = GeneratedMessages.Skip(skip).Take(1000).ToList()).Count > 0)
            {
                await MessageSession.Send(PartitionEndpointName, new RecordJobAdditionalMessages
                {
                    GeneratedMessages = batch,
                    JobId = JobDetails.JobId,
                }).ConfigureAwait(false);
                skip += 1000;
            }
        }

        [When(@"the period end service notifies the job monitoring service to record the start job")]
        public async Task WhenThePeriodEndServiceNotifiesTheJobMonitoringServiceToRecordTheJob()
        {
            await NotifyRecordJob<RecordPeriodEndStartJob>();
        }

        [When(@"the period end service notifies the job monitoring service to record run job")]
        public async Task WhenThePeriodEndServiceNotifiesTheJobMonitoringServiceToRecordRunJob()
        {
            await NotifyRecordJob<RecordPeriodEndRunJob>();
        }

        [When(@"the period end service notifies the job monitoring service to record stop job")]
        public async Task WhenThePeriodEndServiceNotifiesTheJobMonitoringServiceToRecordStopJob()
        {
            await NotifyRecordJob<RecordPeriodEndStopJob>();
        }

        private async Task NotifyRecordJob<T>() where T : RecordPeriodEndJob
        {
            var recordPeriodEndJob = JobDetails as T;
            if (recordPeriodEndJob != null)
            {
                recordPeriodEndJob.GeneratedMessages = GeneratedMessages.Take(1000).ToList();
                await MessageSession.Send(PartitionEndpointName, JobDetails).ConfigureAwait(false);
            }
        }

        [When(@"the final messages for the job are failed to be processed")]
        public async Task WhenTheFinalMessagesForTheJobAreFailedToBeProcessed()
        {
            foreach (var generatedMessage in GeneratedMessages)
            {
                await MessageSession.Send(PartitionEndpointName, new RecordJobMessageProcessingStatus
                {
                    JobId = JobDetails.JobId,
                    MessageName = generatedMessage.MessageName,
                    EndTime = DateTimeOffset.UtcNow,
                    Succeeded = false,
                    Id = generatedMessage.MessageId
                });
            }
        }

        [When(@"Data-Collections confirms the successful completion of the job")]
        public async Task WhenData_CollectionsConfirmsTheSuccessfulCompletionOfTheJob()
        {
            _ = JobDetails as RecordEarningsJob ??
                              throw new InvalidOperationException($"Expected job to be a {nameof(RecordEarningsJob)}");
            await MessageSession.Send(PartitionEndpointName, new RecordEarningsJobSucceeded
            {
                JobId = JobDetails.JobId,
                CollectionPeriod = CollectionPeriod,
                Ukprn = TestSession.Ukprn,
                AcademicYear = AcademicYear,
                IlrSubmissionDateTime = ((RecordEarningsJob)JobDetails).IlrSubmissionTime
            }).ConfigureAwait(false);
        }

        [When(@"Data-Collections confirms the failure of the job")]
        public async Task WhenData_CollectionsConfirmsTheFailureOfTheJob()
        {
            _ = JobDetails as RecordEarningsJob ??
                              throw new InvalidOperationException($"Expected job to be a  {nameof(RecordEarningsJob)}");
            await MessageSession.Send(PartitionEndpointName, new RecordEarningsJobFailed
            {
                JobId = JobDetails.JobId,
                CollectionPeriod = CollectionPeriod,
                Ukprn = TestSession.Ukprn,
                AcademicYear = AcademicYear,
                IlrSubmissionDateTime = ((RecordEarningsJob)JobDetails).IlrSubmissionTime
            }).ConfigureAwait(false);
        }

        [Then(@"the job monitoring service should update the status of the job to show that it has completed with errors")]
        public async Task ThenTheJobMonitoringServiceShouldUpdateTheStatusOfTheJobToShowThatItHasCompletedWithErrors()
        {
            await WaitForIt(
                () =>
                {
                    return DataContext.Jobs.Any(j => j.Id == Job.Id && j.Status == JobStatus.CompletedWithErrors);
                }, $"Status was not updated to Completed for job: {Job.Id}, Dc job id: {JobDetails.JobId}");
        }

        [Then(@"the job monitoring service should record the job")]
        public async Task ThenTheJobMonitoringServiceShouldRecordTheJob()
        {
            await WaitForIt(async () =>
            {
                var job = await DataContext.Jobs.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.DcJobId == JobDetails.JobId);

                if (job == null)
                    return false;
                Job = job;
                Console.WriteLine($"Found job: {Job.Id}, status: {Job.Status}, start time: {job.StartTime}");
                return true;
            }, $"Failed to find job with dc job id: {JobDetails.JobId}");
        }

        [Then(@"the job monitoring service should also record the period end job messages")]
        [Then(@"the job monitoring service should also record the messages generated by earning events service")]
        public async Task ThenTheJobMonitoringServiceShouldAlsoRecordTheMessagesGeneratedByEarningEventsService()
        {
            await WaitForIt(() =>
            {
                foreach (var generatedMessage in GeneratedMessages)
                {
                    if (!DataContext.JobSteps.Any(step =>
                        step.JobId == Job.Id && step.MessageId == generatedMessage.MessageId))
                    {
                        Console.WriteLine($"Failed to find job step {generatedMessage.MessageId} for job: {Job.Id}");
                        return false;
                    }

                    Console.WriteLine($"Found job step: {generatedMessage.MessageId}");
                }

                Console.WriteLine($"Found all expected job steps for job : {Job.Id}, dc job id: {JobDetails.JobId}");
                return true;
            }, $"Failed to find the expected job steps for job: {Job.Id}");
        }

        [Then(@"the job monitoring service should update the status of the job to show that it has completed")]
        public async Task ThenTheJobMonitoringServiceShouldUpdateTheStatusOfTheJobToShowThatItHasCompleted()
        {
            await WaitForIt(() =>
            {
                var job = DataContext.Jobs.AsNoTracking()
                    .FirstOrDefault(x =>
                        x.DcJobId == JobDetails.JobId && x.Status == JobStatus.Completed && x.EndTime != null);

                if (job == null)
                    return false;
                Job = job;
                Console.WriteLine($"Found job: {Job.Id}, status: {Job.Status}, start time: {job.StartTime}");
                return true;
            }, $"Failed to find job with dc job id: {JobDetails.JobId}");
        }

        [Then(@"the monitoring service should record the successful completion of the Data-Collections processes")]
        public async Task ThenTheMonitoringServiceShouldRecordTheSuccessfulCompletionOfTheJob()
        {
            await WaitForIt(() =>
            {
                var job = DataContext.Jobs.AsNoTracking()
                    .FirstOrDefault(x => x.DcJobId == JobDetails.JobId && x.DcJobSucceeded == true);

                if (job == null)
                    return false;

                Job = job;
                Console.WriteLine($"Found job: {Job.Id}, status: {Job.Status}, start time: {job.StartTime}");
                return true;
            }, $"Failed to find job with dc status completed and dc job id: {JobDetails.JobId}");
        }

        [Then(@"when the final messages for the job are successfully processed for the submission job")]
        public async Task WhenTheFinalMessagesForTheJobAreSuccessfullyProcessedForTheSubmissionJob()
        {
            var job = await DataContext.Jobs.SingleOrDefaultAsync(x => x.DcJobId == PeriodEndLargeSubmissionJobId);
            job.Status = JobStatus.Completed;
            job.EndTime = job.DcJobEndTime = DateTimeOffset.UtcNow.AddMinutes(1);
            job.DcJobSucceeded = true;
            await DataContext.SaveChangesAsync();
        }

        [When(@"outstanding submission job times out")]
        public async Task WhenOutstandingSubmissionJobTimesOut()
        {
            var job = await DataContext.Jobs.FirstOrDefaultAsync(x => x.DcJobId == PeriodEndLargeSubmissionJobId);
            job.Status = JobStatus.TimedOut;
            job.EndTime = DateTimeOffset.UtcNow.AddSeconds(30);
            await DataContext.SaveChangesAsync();
        }

        [Then(@"the job monitoring service should update the status of the job to show that it has failed")]
        public async Task ThenTheJobMonitoringServiceShouldUpdateTheStatusOfTheJobToShowThatItHasFailed()
        {
            await WaitForIt(() =>
            {
                var job = DataContext.Jobs.AsNoTracking()
                    .FirstOrDefault(x =>
                        x.DcJobId == JobDetails.JobId && x.Status == JobStatus.CompletedWithErrors && x.EndTime != null);

                if (job == null)
                    return false;
                Job = job;
                Console.WriteLine($"Found job: {Job.Id}, status: {Job.Status}, start time: {job.StartTime}");
                return true;
            }, $"Failed to find completed with errors job with dc job id: {JobDetails.JobId}");
        }

        [Then(@"the job monitoring service should update the status of the job to show that it has timed out")]
        public async Task ThenTheJobMonitoringServiceShouldUpdateTheStatusOfTheJobToShowThatItHasTimedOut()
        {
            await WaitForIt(() =>
            {
                var job = DataContext.Jobs.AsNoTracking()
                    .FirstOrDefault(x =>
                        x.DcJobId == JobDetails.JobId && x.Status == JobStatus.TimedOut && x.EndTime != null);

                if (job == null)
                    return false;
                Job = job;
                Console.WriteLine($"Found job: {Job.Id}, status: {Job.Status}, start time: {job.StartTime}");
                return true;
            }, $"Failed to find timed out job with dc job id: {JobDetails.JobId}");
        }

        [Then(@"the monitoring service should notify other services that the period end start job has failed")]
        public async Task ThenTheMonitoringServiceShouldNotifyOtherServicesThatThePeriodEndStartJobHasFailed()
        {
            await WaitForIt(() => PeriodEndStartFailedHandler.ReceivedEvents.Any(ev => ev.JobId == JobDetails.JobId),
                    $"Failed to receive the period end start job Failed event for job id: {JobDetails.JobId}")
                .ConfigureAwait(false);
        }

        [Then(@"the monitoring service should notify other services that the period end start job has completed successfully")]
        public async Task ThenTheMonitoringServiceShouldNotifyOtherServicesThatThePeriodEndStartJobHasCompletedSuccessfully()
        {
            await WaitForIt(() => PeriodEndStartSuccessHandler.ReceivedEvents.Any(ev => ev.JobId == JobDetails.JobId),
                    $"Failed to receive the period end start job succeeded event for job id: {JobDetails.JobId}")
                .ConfigureAwait(false);
        }

        [Then(@"the monitoring service should notify other services that the period end run job has completed successfully")]
        public async Task ThenTheMonitoringServiceShouldNotifyOtherServicesThatThePeriodEndRunJobHasCompletedSuccessfully()
        {
            await WaitForIt(() => PeriodEndRunSuccessHandler.ReceivedEvents.Any(ev => ev.JobId == JobDetails.JobId),
                    $"Failed to receive the period end run job succeeded event for job id: {JobDetails.JobId}")
                .ConfigureAwait(false);
        }

        [Then(@"the monitoring service should notify other services that the period end stop job has completed successfully")]
        public async Task ThenTheMonitoringServiceShouldNotifyOtherServicesThatThePeriodEndStopJobHasCompletedSuccessfully()
        {
            await WaitForIt(() => PeriodEndStopSuccessHandler.ReceivedEvents.Any(ev => ev.JobId == JobDetails.JobId),
                    $"Failed to receive the period end stop job succeeded event for job id: {JobDetails.JobId}")
                .ConfigureAwait(false);
        }

        [Then(@"the monitoring service should notify other services that the job has completed successfully")]
        public async Task ThenTheMonitoringServiceShouldNotifyOtherServicesThatTheJobHasCompletedSuccessfully()
        {
            await WaitForIt(() => SubmissionJobSucceededHandler.ReceivedEvents.Any(ev => ev.JobId == JobDetails.JobId),
                    $"Failed to receive the submission job succeeded event for job id: {JobDetails.JobId}")
                .ConfigureAwait(false);
        }

        [Then(@"the monitoring service should record the failure of the Data-Collections processes")]
        public async Task ThenTheMonitoringServiceShouldRecordTheFailureOfTheData_CollectionsProcesses()
        {
            await WaitForIt(() =>
            {
                var job = DataContext.Jobs.AsNoTracking()
                    .FirstOrDefault(x =>
                        x.DcJobId == JobDetails.JobId && x.DcJobSucceeded == false);

                if (job == null)
                    return false;

                Job = job;
                Console.WriteLine($"Found job: {Job.Id}, status: {Job.Status}, start time: {job.StartTime}");
                return true;
            }, $"Failed to find job with dc status failed and dc job id: {JobDetails.JobId}");
        }

        [Then(@"the monitoring service should notify other services that the job has failed")]
        public async Task ThenTheMonitoringServiceShouldNotifyOtherServicesThatTheJobHasFailed()
        {
            await WaitForIt(() => SubmissionJobFailedHandler.ReceivedEvents.Any(ev => ev.JobId == JobDetails.JobId),
                    $"Failed to receive the submission job failed event for job id: {JobDetails.JobId}")
                .ConfigureAwait(false);
        }

    }
}
