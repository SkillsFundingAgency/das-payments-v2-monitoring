using Microsoft.EntityFrameworkCore;
using SFA.DAS.Payments.Monitoring.Metrics.Data;
using SFA.DAS.Payments.Monitoring.Tests.Specs.Configuration;

namespace SFA.DAS.Payments.Monitoring.Tests.Specs.Data
{
    public class SqlServerMetricsQueryDataContext : MetricsQueryDataContext
    {
        public SqlServerMetricsQueryDataContext() : base(
            new DbContextOptionsBuilder()
                .UseSqlServer(TestConfiguration.PaymentsConnectionString, options => options.CommandTimeout(600))
                .Options)
        {
        }

        public Task<int> DeletePaymentsForUkprn(long ukprn)
        {
            return Database.ExecuteSqlRawAsync("delete from Payments2.Payment where Ukprn = {0}", ukprn);
        }
    }
}
