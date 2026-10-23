using Reqnroll;
using SFA.DAS.Payments.Monitoring.Tests.Specs.Drivers;

namespace SFA.DAS.Payments.Monitoring.Tests.Specs.Hooks
{
    [Binding]
    public class DatabaseHooks
    {
        private readonly PeriodEndMetricsRepositoryDriver periodEndMetricsRepositoryDriver;
        private readonly AlreadyPaidDataLockTotalsDriver alreadyPaidDataLockTotalsDriver;

        public DatabaseHooks(
            PeriodEndMetricsRepositoryDriver periodEndMetricsRepositoryDriver,
            AlreadyPaidDataLockTotalsDriver alreadyPaidDataLockTotalsDriver)
        {
            this.periodEndMetricsRepositoryDriver = periodEndMetricsRepositoryDriver;
            this.alreadyPaidDataLockTotalsDriver = alreadyPaidDataLockTotalsDriver;
        }

        [BeforeScenario]
        public async Task RemoveTestDataBeforeScenario()
        {
            await RemoveTestData();
            await alreadyPaidDataLockTotalsDriver.AddSuccessfulJobForScenario();
        }
        

        [AfterScenario]
        public async Task RemoveTestDataAfterScenario()
        {
            await RemoveTestData();
        }

        private async Task RemoveTestData()
        {
            await alreadyPaidDataLockTotalsDriver.RemoveTestData();
            await periodEndMetricsRepositoryDriver.RemoveTestData();
        }
    }
}
