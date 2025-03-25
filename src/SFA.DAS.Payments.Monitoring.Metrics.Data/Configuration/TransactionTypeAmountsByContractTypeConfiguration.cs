using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SFA.DAS.Payments.Monitoring.Metrics.Model;

namespace SFA.DAS.Payments.Monitoring.Metrics.Data.Configuration
{
    public class
        TransactionTypeAmountsByContractTypeConfiguration : IEntityTypeConfiguration<
        TransactionTypeAmountsByContractType>
    {
        public void Configure(EntityTypeBuilder<TransactionTypeAmountsByContractType> builder)
        {
            builder.HasNoKey();
        }
    }
}
