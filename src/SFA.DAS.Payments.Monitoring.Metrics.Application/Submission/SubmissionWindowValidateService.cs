﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using SFA.DAS.Payments.Application.Infrastructure.Logging;
using SFA.DAS.Payments.Application.Infrastructure.Telemetry;
using SFA.DAS.Payments.Monitoring.Metrics.Domain.Submission;
using SFA.DAS.Payments.Monitoring.Metrics.Model.Submission;

namespace SFA.DAS.Payments.Monitoring.Metrics.Application.Submission
{
    public interface ISubmissionWindowValidationService
    {
        Task<SubmissionsSummaryModel> ValidateSubmissionWindow(long jobId, short academicYear, byte collectionPeriod, CancellationToken cancellationToken);
        Task EstimateSubmissionWindowMetrics();
    }

    public class SubmissionWindowValidationService : ISubmissionWindowValidationService
    {
        private readonly IPaymentLogger logger;
        private readonly ISubmissionMetricsRepository submissionMetricsRepository;
        private readonly ISubmissionJobsRepository submissionJobsRepository;
        private readonly ISubmissionsSummary submissionsSummary;
        private readonly ITelemetry telemetry;

        public SubmissionWindowValidationService(IPaymentLogger logger, ISubmissionMetricsRepository submissionMetricsRepository, ISubmissionJobsRepository submissionJobsRepository, ISubmissionsSummary submissionsSummary, ITelemetry telemetry)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.submissionMetricsRepository = submissionMetricsRepository ?? throw new ArgumentNullException(nameof(submissionMetricsRepository));
            this.submissionJobsRepository = submissionJobsRepository ?? throw new ArgumentNullException(nameof(submissionJobsRepository));
            this.submissionsSummary = submissionsSummary ?? throw new ArgumentNullException(nameof(submissionsSummary));
            this.telemetry = telemetry ?? throw new ArgumentNullException(nameof(telemetry));
        }

        public async Task EstimateSubmissionWindowMetrics()
        {
            var latestCollection = await submissionJobsRepository.GetLatestCollectionPeriod();
            await ValidateSubmissionWindow(0, latestCollection.AcademicYear, latestCollection.CollectionPeriod, CancellationToken.None);
        }

        public async Task<SubmissionsSummaryModel> ValidateSubmissionWindow(long jobId, short academicYear, byte collectionPeriod, CancellationToken cancellationToken)
        {
            var isEstimatingMetrics = jobId == 0;
            var logMessage = isEstimatingMetrics ? "estimating" : "building";

            try
            {
                logger.LogDebug($"Started {logMessage} metrics for job: {jobId}, Academic year: {academicYear}, Collection period: {collectionPeriod}");

                var stopwatch = Stopwatch.StartNew();

                var submissionSummaries = await submissionMetricsRepository.GetSubmissionsSummaryMetrics(jobId, academicYear, collectionPeriod, cancellationToken);

                var metrics = submissionsSummary.GetMetrics(jobId, academicYear, collectionPeriod, submissionSummaries);

                var collectionPeriodTolerance = await submissionMetricsRepository.GetCollectionPeriodTolerance(collectionPeriod, academicYear, cancellationToken);

                submissionsSummary.CalculateIsWithinTolerance(collectionPeriodTolerance?.SubmissionToleranceLower, collectionPeriodTolerance?.SubmissionToleranceUpper);

                cancellationToken.ThrowIfCancellationRequested();

                var dataDuration = stopwatch.ElapsedMilliseconds;

                logger.LogDebug($"finished getting data from databases for job: {jobId}, Took: {dataDuration}ms.");

                if (!isEstimatingMetrics) await submissionMetricsRepository.SaveSubmissionsSummaryMetrics(metrics, cancellationToken);

                stopwatch.Stop();

                SendTelemetry(metrics, stopwatch.ElapsedMilliseconds);

                logger.LogInfo($"Finished {logMessage} Submissions Summary Metrics for job: {jobId}, Academic year: {academicYear}, Collection period: {collectionPeriod}. Took: {stopwatch.ElapsedMilliseconds}ms");

                return metrics;
            }
            catch (Exception e)
            {
                logger.LogWarning($"Error {logMessage} the Submissions Summary metrics report for job: {jobId}, Error: {e}");
                throw;
            }
        }

        private void SendTelemetry(SubmissionsSummaryModel metrics, long reportGenerationDuration)
        {
            var isEstimatingMetrics = metrics?.JobId == 0;
            var logMessage = isEstimatingMetrics ? "Estimating" : "Generating";

            if (metrics == null) return;

            var properties = new Dictionary<string, string>
            {
                { TelemetryKeys.JobId, metrics.JobId.ToString() },
                { TelemetryKeys.CollectionPeriod, metrics.CollectionPeriod.ToString() },
                { TelemetryKeys.AcademicYear, metrics.AcademicYear.ToString() },
                { "IsWithinTolerance", metrics.IsWithinTolerance.ToString() }
            };

            var submissionMetrics = metrics.SubmissionMetrics;
            var dasEarnings = metrics.DasEarnings;
            var dcEarnings = metrics.DcEarnings;

            var stats = new Dictionary<string, double>
            {
                { "ReportGenerationDuration", reportGenerationDuration },

                { "Percentage" ,                              (double) submissionMetrics.Percentage },
                { "ContractType1Percentage" ,                 (double) submissionMetrics.PercentageContractType1 },
                { "ContractType2Percentage" ,                 (double) submissionMetrics.PercentageContractType2 },

                { "DifferenceTotal" ,                         (double) submissionMetrics.DifferenceTotal },
                { "DifferenceContractType1" ,                 (double) submissionMetrics.DifferenceContractType1 },
                { "DifferenceContractType2" ,                 (double) submissionMetrics.DifferenceContractType2 },

                { "ContractAmountTotal" ,                     (double) submissionMetrics.Total },
                { "ContractType1Amount" ,                     (double) submissionMetrics.ContractType1 },
                { "ContractType2Amount" ,                     (double) submissionMetrics.ContractType2 },

                { "DasEarningsPercentage" ,                   (double) dasEarnings.Percentage },
                { "DasEarningsPercentageContractType1" ,      (double) dasEarnings.PercentageContractType1 },
                { "DasEarningsPercentageContractType2" ,      (double) dasEarnings.PercentageContractType2 },

                { "DasEarningsDifferenceTotal" ,              (double) dasEarnings.DifferenceTotal },
                { "DasEarningsDifferenceContractType1" ,      (double) dasEarnings.DifferenceContractType1 },
                { "DasEarningsDifferenceContractType2" ,      (double) dasEarnings.DifferenceContractType2 },

                { "DasEarningsTotal" ,                        (double) dasEarnings.Total },
                { "DasEarningsContractType1Total" ,           (double) dasEarnings.ContractType1 },
                { "DasEarningsContractType2Total" ,           (double) dasEarnings.ContractType2 },

                { "DcEarningsTotal" ,                         (double) dcEarnings.Total },
                { "DcEarningsContractType1Total" ,            (double) dcEarnings.ContractType1 },
                { "DcEarningsContractType2Total" ,            (double) dcEarnings.ContractType2 },

                { "DataLockedEarnings" ,                      (double) metrics.TotalDataLockedEarnings },
                { "DataLockedAlreadyPaidAmount" ,             (double) metrics.AlreadyPaidDataLockedEarnings },
                { "DataLockedAdjustedAmount" ,                (double) metrics.AdjustedDataLockedEarnings },

                { "DataLockedCountDLock1",     metrics.DataLockMetricsTotals.Amounts.DataLock1 },
                { "DataLockedCountDLock2",     metrics.DataLockMetricsTotals.Amounts.DataLock2 },
                { "DataLockedCountDLock3",     metrics.DataLockMetricsTotals.Amounts.DataLock3 },
                { "DataLockedCountDLock4",     metrics.DataLockMetricsTotals.Amounts.DataLock4 },
                { "DataLockedCountDLock5",     metrics.DataLockMetricsTotals.Amounts.DataLock5 },
                { "DataLockedCountDLock6",     metrics.DataLockMetricsTotals.Amounts.DataLock6 },
                { "DataLockedCountDLock7",     metrics.DataLockMetricsTotals.Amounts.DataLock7 },
                { "DataLockedCountDLock8",     metrics.DataLockMetricsTotals.Amounts.DataLock8 },
                { "DataLockedCountDLock9",     metrics.DataLockMetricsTotals.Amounts.DataLock9 },
                { "DataLockedCountDLock10",     metrics.DataLockMetricsTotals.Amounts.DataLock10 },
                { "DataLockedCountDLock11",     metrics.DataLockMetricsTotals.Amounts.DataLock11 },
                { "DataLockedCountDLock12",     metrics.DataLockMetricsTotals.Amounts.DataLock12 },

                { "HeldBackCompletionPaymentsTotal" ,         (double) metrics.HeldBackCompletionPayments.Total },
                { "HeldBackCompletionPaymentsContractType1" , (double) metrics.HeldBackCompletionPayments.ContractType1 },
                { "HeldBackCompletionPaymentsContractType2" , (double) metrics.HeldBackCompletionPayments.ContractType2 },

                { "RequiredPaymentsTotal" ,                   (double) metrics.RequiredPayments.Total },
                { "RequiredPaymentsAct1Total" ,               (double) metrics.RequiredPayments.ContractType1 },
                { "RequiredPaymentsAc2Total" ,                (double) metrics.RequiredPayments.ContractType2 },

                { "YearToDatePaymentsTotal" ,                 (double) metrics.YearToDatePayments.Total },
                { "YearToDatePaymentsContractType1Total",     (double) metrics.YearToDatePayments.ContractType1 },
                { "YearToDatePaymentsContractType2Total",     (double) metrics.YearToDatePayments.ContractType2 },

                { "RequiredPaymentsDasEarningsPercentageComparison" ,  Math.Round(((double) (metrics.YearToDatePayments.Total + metrics.RequiredPayments.Total) / (double) metrics.DasEarnings.Total) * 100, 2) },
            };

            telemetry.TrackEvent($"Finished {logMessage} Submissions Summary Metrics", properties, stats);
        }
    }
}