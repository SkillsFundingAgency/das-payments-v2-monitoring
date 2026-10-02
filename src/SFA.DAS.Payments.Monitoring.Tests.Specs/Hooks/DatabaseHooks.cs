using Reqnroll;
using SFA.DAS.Payments.Monitoring.Tests.Specs.Drivers;

namespace SFA.DAS.Payments.Monitoring.Tests.Specs.Hooks
{
    [Binding]
    public class DatabaseHooks
    {
        private readonly PeriodEndMetricsDriver driver;

        public DatabaseHooks(PeriodEndMetricsDriver driver)
        {
            this.driver = driver;
        }

        [BeforeScenario]
        public async Task RemoveTestDataBeforeScenario()
        {
            await driver.RemoveTestPayments();
        }

        [AfterScenario]
        public async Task RemoveTestDataAfterScenario()
        {
            await driver.RemoveTestPayments();
        }
    }
}
