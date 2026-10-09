using FluentAssertions;
using Reqnroll;
using SFA.DAS.Payments.Model.Core.Entities;
using SFA.DAS.Payments.Monitoring.Tests.Specs.Drivers;
using SFA.DAS.Payments.Monitoring.Tests.Specs.Support;

namespace SFA.DAS.Payments.Monitoring.Tests.Specs.StepDefinitions
{
    [Binding]
    public class PeriodEndMetricsExcludeDasEarningsSteps
    {
        //Coursetype technically not needed but I think it helps clear things up, learning and milestone1 should be sufficient

        private readonly PeriodEndMetricsRepositoryDriver periodEndMetricsRepositoryDriver;
        private readonly AlreadyPaidDataLockTotalsDriver alreadyPaidDataLockTotalsDriver;

        public PeriodEndMetricsExcludeDasEarningsSteps(
            PeriodEndMetricsRepositoryDriver periodEndMetricsRepositoryDriver,
            AlreadyPaidDataLockTotalsDriver alreadyPaidDataLockTotalsDriver)
        {
            this.periodEndMetricsRepositoryDriver = periodEndMetricsRepositoryDriver;
            this.alreadyPaidDataLockTotalsDriver = alreadyPaidDataLockTotalsDriver;
        }

        [Given("we are generating period end metrics")]
        public void GivenWeAreGeneratingPeriodEndMetrics()
        {
        }

        [Given("payments have been generated for Apprenticeship and GSO Short Course payments")]
        public async Task GivenPaymentsIncludeApprenticeshipAndGsoShortCoursePayments()
        {
            //SLD never uses GSL, so it goes through DAS
            //Put payment amounts in here, to eastablish expected amount more easily
            await periodEndMetricsRepositoryDriver.AddPaymentForPlatform(
                transactionType: TransactionType.Learning,
                courseType: CourseType.Apprenticeship,
                platform: FundingPlatformType.SubmitLearnerData,
                amount: 2000m);

            await periodEndMetricsRepositoryDriver.AddPaymentForPlatform(
                transactionType: TransactionType.Learning,
                courseType: CourseType.Apprenticeship,
                platform: FundingPlatformType.DigitalApprenticeshipService,
                amount: 50m,
                learnerUln: TestData.DasLearnerUln);

            await periodEndMetricsRepositoryDriver.AddPaymentForPlatform(
                transactionType: TransactionType.Milestone1,
                courseType: CourseType.ShortCourse,
                platform: FundingPlatformType.DigitalApprenticeshipService,
                amount: 50m,
                learnerUln: TestData.DasLearnerUln);
        }

        [Given("payments have been generated for Apprenticeships")]
        public async Task GivenPaymentsHaveBeenGeneratedForApprenticeships()
        {
            await periodEndMetricsRepositoryDriver.AddPaymentForPlatform(
                transactionType: TransactionType.Learning,
                courseType: CourseType.Apprenticeship,
                platform: FundingPlatformType.SubmitLearnerData,
                amount: 2000m);

            await periodEndMetricsRepositoryDriver.AddPaymentForPlatform(
                transactionType: TransactionType.Learning,
                courseType: CourseType.Apprenticeship,
                platform: FundingPlatformType.DigitalApprenticeshipService,
                amount: 50m,
                learnerUln: TestData.DasLearnerUln);
        }

        [Given("legacy SLD payments and DAS payments have been generated")]
        public async Task GivenLegacySldPaymentsAndDasPaymentsHaveBeenGenerated()
        {
            await periodEndMetricsRepositoryDriver.AddPaymentForPlatform(
                transactionType: TransactionType.Learning,
                courseType: CourseType.Apprenticeship,
                platform: null,
                amount: 2000m);

            await periodEndMetricsRepositoryDriver.AddPaymentForPlatform(
                transactionType: TransactionType.Learning,
                courseType: CourseType.Apprenticeship,
                platform: FundingPlatformType.DigitalApprenticeshipService,
                amount: 500m,
                learnerUln: TestData.DasLearnerUln);
        }

        [Given("historical payments from SLD and DAS are associated with data-lock events")]
        public async Task GivenHistoricalPaymentsFromSldAndDasAreAssociatedWithDataLockEvents()
        {
            //the 100m data-lock amount is arbitrary, just needs to be non-zero to test the logic
            //The 3000m and 500m are the amounts that will be used to test the metrics total and already-paid data-lock earnings.
            await alreadyPaidDataLockTotalsDriver.AddHistoricalPaymentWithDataLockEvent(
                platform: FundingPlatformType.SubmitLearnerData,
                paymentAmount: 3000m,
                dataLockedAmount: 100m);

            await alreadyPaidDataLockTotalsDriver.AddHistoricalPaymentWithDataLockEvent(
                platform: FundingPlatformType.DigitalApprenticeshipService,
                paymentAmount: 500m,
                dataLockedAmount: 100m);
        }

        [When("the metrics are generated for DC\\/SLD")]
        public async Task WhenTheMetricsAreGeneratedForDcSld()
        {
            await periodEndMetricsRepositoryDriver.GenerateMetrics();
        }

        [When("GetAlreadyPaidDataLockProviderTotals is called")]
        public async Task WhenGetAlreadyPaidDataLockProviderTotalsIsCalled()
        {
            await alreadyPaidDataLockTotalsDriver.GetAlreadyPaidDataLockProviderTotal();
        }

        [Then("only SLD earnings are included in the funding-source metrics")]
        public void ThenOnlySldEarningsAreIncludedInTheMetrics()
        {
            var result = periodEndMetricsRepositoryDriver.FundingSourceAmounts.Should().ContainSingle().Subject;

            result.Ukprn.Should().Be(TestData.Ukprn);
            result.FundingSource1.Should().Be(2000m);
            result.FundingSource2.Should().Be(0m);
            result.FundingSource3.Should().Be(0m);
            result.FundingSource4.Should().Be(0m);
            result.FundingSource5.Should().Be(0m);
            result.Total.Should().Be(2000m);
        }

        [Then("only SLD learning payments are included in transaction-type metrics")]
        public void ThenOnlySldLearningPaymentsAreIncludedInTransactionMetrics()
        {
            var result = periodEndMetricsRepositoryDriver.TransactionTypeAmounts.Should().ContainSingle().Subject;

            result.TransactionType1.Should().Be(2000m);
            result.Total.Should().Be(2000m);
        }

        [Then("only SLD learners are included in the in-learning metrics")]
        public void ThenOnlySldLearnersAreIncludedInTheInLearningMetrics()
        {
            var result = periodEndMetricsRepositoryDriver.InLearningTotals.Should().ContainSingle().Subject;

            result.InLearningCount.Should().Be(1);
        }

        [Then("only SLD payments are included in year-to-date metrics")]
        public void ThenOnlySldPaymentsAreIncludedInYearToDateMetrics()
        {
            var result = periodEndMetricsRepositoryDriver.YearToDatePaymentAmounts.Should().ContainSingle().Subject;

            result.ContractType1.Should().Be(2000m);
            result.Total.Should().Be(2000m);
        }

        [Then("only SLD learner payments are returned")]
        public void ThenOnlySldLearnerPaymentsAreReturned()
        {
            var result = periodEndMetricsRepositoryDriver.LearnerPaymentAmounts.Should().ContainSingle().Subject;

            result.LearnerUln.Should().Be(TestData.LearnerUln);
            result.ContractType1.Should().Be(2000m);
            result.Total.Should().Be(2000m);
        }

        [Then("only historical SLD payments are included in already-paid data-lock earnings")]
        public void ThenOnlyHistoricalSldPaymentsAreIncludedInAlreadyPaidDataLockEarnings()
        {
            alreadyPaidDataLockTotalsDriver.AlreadyPaidDataLockedEarnings.Should().Be(3000m);
        }

    }
}
