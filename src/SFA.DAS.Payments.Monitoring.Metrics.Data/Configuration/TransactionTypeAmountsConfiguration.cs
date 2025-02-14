using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SFA.DAS.Payments.Monitoring.Metrics.Model;

namespace SFA.DAS.Payments.Monitoring.Metrics.Data.Configuration
{

    public class
        TransactionTypeAmountsConfiguration : IEntityTypeConfiguration<
        TransactionTypeAmounts>
    {
        public void Configure(EntityTypeBuilder<TransactionTypeAmounts> builder)
        {
            builder.HasNoKey();
        }
    }
}
