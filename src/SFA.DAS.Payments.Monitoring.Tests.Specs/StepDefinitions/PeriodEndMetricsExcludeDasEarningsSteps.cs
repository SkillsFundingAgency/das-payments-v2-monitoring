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
        private readonly PeriodEndMetricsDriver driver;

        public PeriodEndMetricsExcludeDasEarningsSteps(PeriodEndMetricsDriver driver)
        {
            this.driver = driver;
        }

        [Given("we are generating period end or submission metrics")]
        public void GivenWeAreGeneratingPeriodEndOrSubmissionMetrics()
        {
        }

        [Given("payments include Apprenticeship and GSO Short Course payments")]
        public async Task GivenPaymentsIncludeApprenticeshipAndGsoShortCoursePayments()
        {
            await driver.AddPaymentsForBothPlatforms(TransactionType.Learning);
            await driver.AddPaymentsForBothPlatforms(TestData.ShortCourseMilestone);
        }

        [Given("payments have been generated for Apprenticeships")]
        public async Task GivenPaymentsHaveBeenGeneratedForApprenticeships()
        {
            await driver.AddPaymentsForBothPlatforms(TransactionType.Learning);
        }

        [When("the metrics are generated for DC\\/SLD")]
        public async Task WhenTheMetricsAreGeneratedForDcSld()
        {
            await driver.GenerateMetrics();
        }

        [Then("the payments generated from SLD Earnings are included in the metrics")]
        public void ThenThePaymentsGeneratedFromSldEarningsAreIncludedInTheMetrics()
        {
            driver.ExpectedSldTotal.Should().BeGreaterThan(0);
            driver.MetricsTotal.Should().BeGreaterOrEqualTo(driver.ExpectedSldTotal);
        }

        [Then("the payments generated from DAS Earnings are not included in the metrics")]
        public void ThenThePaymentsGeneratedFromDasEarningsAreNotIncludedInTheMetrics()
        {
            driver.ExcludedDasTotal.Should().BeGreaterThan(0);
            driver.MetricsTotal.Should().Be(driver.ExpectedSldTotal);
        }
    }
}
