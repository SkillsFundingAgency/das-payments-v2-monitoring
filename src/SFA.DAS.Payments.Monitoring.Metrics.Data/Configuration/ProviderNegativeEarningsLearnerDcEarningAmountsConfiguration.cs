using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SFA.DAS.Payments.Monitoring.Metrics.Model;

namespace SFA.DAS.Payments.Monitoring.Metrics.Data.Configuration
{
    public class
        ProviderNegativeEarningsLearnerDcEarningAmountsConfiguration : IEntityTypeConfiguration<
        ProviderNegativeEarningsLearnerDcEarningAmounts>
    {
        public void Configure(EntityTypeBuilder<ProviderNegativeEarningsLearnerDcEarningAmounts> builder)
        {
            builder.HasNoKey();
        }
    }
}
