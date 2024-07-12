using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using SFA.DAS.Payments.Monitoring.Metrics.Model;

namespace SFA.DAS.Payments.Monitoring.Metrics.Data.Configuration
{
    public class ProviderNegativeEarningsLearnerDataLockFundingLineTypeAmountsConfiguration : IEntityTypeConfiguration<ProviderNegativeEarningsLearnerDataLockFundingLineTypeAmounts>
    {
        public void Configure(EntityTypeBuilder<ProviderNegativeEarningsLearnerDataLockFundingLineTypeAmounts> builder)
        {
            builder.HasNoKey();
        }

}
}
