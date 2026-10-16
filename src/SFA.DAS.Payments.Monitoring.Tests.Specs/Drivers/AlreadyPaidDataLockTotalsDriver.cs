using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SFA.DAS.Payments.Model.Core;
using SFA.DAS.Payments.Model.Core.Entities;
using SFA.DAS.Payments.Monitoring.Tests.Specs.Data;
using SFA.DAS.Payments.Monitoring.Tests.Specs.Support;

namespace SFA.DAS.Payments.Monitoring.Tests.Specs.Drivers
{
    public class AlreadyPaidDataLockTotalsDriver : IDisposable
    {
        private readonly SqlServerMetricsQueryDataContext dataContext = new SqlServerMetricsQueryDataContext();
        private readonly List<Guid> dataLockEventIds = new List<Guid>();
        private long? successfulJobId;

        public async Task AddSuccessfulJobForScenario()
        {
            successfulJobId = Random.Shared.NextInt64(1, long.MaxValue);
            await AddSuccessfulJob(successfulJobId.Value);
        }

        public async Task AddHistoricalPaymentWithDataLockEvent(FundingPlatformType platform, decimal paymentAmount, decimal dataLockedAmount)
        {
            var jobId = successfulJobId.Value;

            var payment = CreateHistoricalPayment(platform, paymentAmount, jobId);
            var eventId = Guid.NewGuid();
            dataLockEventIds.Add(eventId);

            await AddDataLockEvent(payment, eventId, jobId, dataLockedAmount);
            dataContext.Add(payment);
            await dataContext.SaveChangesAsync();
        }

        public async Task GetAlreadyPaidDataLockProviderTotal()
        {
            var results = await dataContext.GetAlreadyPaidDataLockProviderTotals(
                TestData.AcademicYear,
                TestData.CollectionPeriod,
                CancellationToken.None);

            AlreadyPaidDataLockedEarnings = results.Single(x => x.Ukprn == TestData.Ukprn).Total;
        }

        public decimal AlreadyPaidDataLockedEarnings { get; private set; }

        public async Task RemoveTestData()
        {
            foreach (var eventId in dataLockEventIds)
            {
                await dataContext.Database.ExecuteSqlRawAsync(
                    "DELETE FROM Payments2.DataLockEventNonPayablePeriod WHERE DataLockEventId = {0}",
                    eventId);

                await dataContext.Database.ExecuteSqlRawAsync(
                    "DELETE FROM Payments2.DataLockEvent WHERE EventId = {0}",
                    eventId);
            }

            dataLockEventIds.Clear();
            await dataContext.DeletePaymentsForUkprn(TestData.Ukprn);

            if (successfulJobId.HasValue)
            {
                await dataContext.Database.ExecuteSqlRawAsync(
                    "DELETE FROM Payments2.Job WHERE DcJobId = {0}",
                    successfulJobId.Value);
            }

            successfulJobId = null;
        }

        private async Task AddSuccessfulJob(long jobId)
        {
            await dataContext.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO [Payments2].[Job]
                (
                    [StartTime],
                    [EndTime],
                    [Status],
                    [JobType],
                    [DCJobId],
                    [Ukprn],
                    [IlrSubmissionTime],
                    [AcademicYear],
                    [CollectionPeriod],
                    [DCJobSucceeded],
                    [DCJobEndTime]
                )
                VALUES
                (
                    {DateTime.UtcNow},
                    {DateTime.UtcNow},
                    {(byte)2},
                    {(byte)1},
                    {jobId},
                    {TestData.Ukprn},
                    {DateTime.UtcNow},
                    {TestData.AcademicYear},
                    {TestData.CollectionPeriod},
                    {true},
                    {DateTime.UtcNow}
                )
                """);
        }

        public void Dispose()
        {
            dataContext.Dispose();
        }

        private static PaymentModel CreateHistoricalPayment(FundingPlatformType platform, decimal amount, long jobId)
        {
            return new PaymentModel
            {
                JobId = jobId,
                EventId = Guid.NewGuid(),
                EventTime = DateTimeOffset.UtcNow,
                Ukprn = TestData.Ukprn,
                LearnerUln = TestData.LearnerUln,
                CollectionPeriod = new CollectionPeriod
                {
                    AcademicYear = (short)(TestData.AcademicYear - 1),
                    Period = 12
                },
                DeliveryPeriod = TestData.CollectionPeriod,
                LearnerReferenceNumber = platform == FundingPlatformType.SubmitLearnerData ? "sld-learner" : "das-learner",
                LearningAimFundingLineType = "19+ Apprenticeship (From May 2017) Non-Levy Contract (non-procured)",
                LearningAimReference = "lar",
                LearningAimProgrammeType = 1,
                LearningAimStandardCode = 1,
                LearningAimFrameworkCode = 1,
                LearningAimPathwayCode = 1,
                PriceEpisodeIdentifier = "price-episode",
                ReportingAimFundingLineType = "reporting-line",
                IlrSubmissionDateTime = DateTime.UtcNow,
                StartDate = DateTime.UtcNow.Date,
                SfaContributionPercentage = 0.9m,
                AccountId = 123456789,
                TransferSenderAccountId = 123456789,
                CompletionAmount = amount,
                CompletionStatus = 1,
                InstalmentAmount = amount,
                ActualEndDate = DateTime.UtcNow,
                NumberOfInstalments = 12,
                PlannedEndDate = DateTime.UtcNow,
                ApprenticeshipId = 800L,
                ApprenticeshipEmployerType = ApprenticeshipEmployerType.Levy,
                ContractType = ContractType.Act1,
                FundingSource = FundingSourceType.Levy,
                TransactionType = TransactionType.Learning,
                FundingPlatformType = platform,
                CourseType = CourseType.Apprenticeship,
                LearningType = LearningType.Apprenticeship,
                CourseCode = "123456",
                Amount = amount
            };
        }

        private async Task AddDataLockEvent(PaymentModel payment, Guid eventId, long jobId, decimal dataLockedAmount)
        {
            const byte dataLockCollectionPeriod = 13;
            var nonPayablePeriodId = Guid.NewGuid();

            await dataContext.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO [Payments2].[DataLockEvent]
                (
                    [EventId],
                    [EarningEventId],
                    [Ukprn],
                    [ContractType],
                    [CollectionPeriod],
                    [AcademicYear],
                    [LearnerReferenceNumber],
                    [LearnerUln],
                    [LearningAimReference],
                    [LearningAimProgrammeType],
                    [LearningAimStandardCode],
                    [LearningAimFrameworkCode],
                    [LearningAimPathwayCode],
                    [LearningAimFundingLineType],
                    [IlrSubmissionDateTime],
                    [IsPayable],
                    [JobId],
                    [EventTime]
                )
                VALUES
                (
                    {eventId},
                    {Guid.NewGuid()},
                    {payment.Ukprn},
                    {(byte)ContractType.Act1},
                    {dataLockCollectionPeriod},
                    {payment.CollectionPeriod.AcademicYear},
                    {payment.LearnerReferenceNumber},
                    {payment.LearnerUln},
                    {payment.LearningAimReference},
                    {payment.LearningAimProgrammeType},
                    {payment.LearningAimStandardCode},
                    {payment.LearningAimFrameworkCode},
                    {payment.LearningAimPathwayCode},
                    {payment.LearningAimFundingLineType},
                    {payment.IlrSubmissionDateTime},
                    {false},
                    {jobId},
                    {DateTimeOffset.UtcNow}
                )
                """);

            await dataContext.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO [Payments2].[DataLockEventNonPayablePeriod]
                (
                    [DataLockEventId],
                    [DataLockEventNonPayablePeriodId],
                    [TransactionType],
                    [DeliveryPeriod],
                    [Amount],
                    [AcademicYear],
                    [CollectionPeriod]
                )
                VALUES
                (
                    {eventId},
                    {nonPayablePeriodId},
                    {(byte)payment.TransactionType},
                    {payment.DeliveryPeriod},
                    {dataLockedAmount},
                    {payment.CollectionPeriod.AcademicYear},
                    {dataLockCollectionPeriod}
                )
                """);
        }
    }
}
