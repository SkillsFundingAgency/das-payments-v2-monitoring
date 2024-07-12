using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using static SFA.DAS.Payments.Monitoring.Metrics.Data.MetricsQueryDataContext;

namespace SFA.DAS.Payments.Monitoring.Metrics.Data.Configuration
{
    public class PeriodEndDataLockCountConfiguration : IEntityTypeConfiguration<PeriodEndDataLockCount>
    {
        public void Configure(EntityTypeBuilder<PeriodEndDataLockCount> builder)
        {
            builder.HasNoKey();
        }
    }
}
