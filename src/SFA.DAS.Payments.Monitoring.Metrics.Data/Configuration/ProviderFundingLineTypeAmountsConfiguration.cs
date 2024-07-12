using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using SFA.DAS.Payments.Monitoring.Metrics.Model;

namespace SFA.DAS.Payments.Monitoring.Metrics.Data.Configuration
{
    public class ProviderFundingLineTypeAmountsConfiguration : IEntityTypeConfiguration<ProviderFundingLineTypeAmounts>
    {
        public void Configure(EntityTypeBuilder<ProviderFundingLineTypeAmounts> builder)
        {
            builder.HasNoKey();
        }
    }
}
