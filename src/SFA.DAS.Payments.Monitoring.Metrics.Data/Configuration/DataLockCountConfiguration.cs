using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static SFA.DAS.Payments.Monitoring.Metrics.Data.MetricsQueryDataContext;

namespace SFA.DAS.Payments.Monitoring.Metrics.Data.Configuration
{
    public class DataLockCountConfiguration : IEntityTypeConfiguration<DataLockCount>
    {
        public void Configure(EntityTypeBuilder<DataLockCount> builder)
        {
            builder.HasNoKey();
        }
    }
}