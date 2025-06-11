using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using SFA.DAS.Payments.Monitoring.Metrics.Model;

namespace SFA.DAS.Payments.Monitoring.Metrics.Data.Configuration
{
    public class ContractTypeAmountsConfiguration : IEntityTypeConfiguration<ContractTypeAmounts>
    {
        public void Configure(EntityTypeBuilder<ContractTypeAmounts> builder)
        {
            builder.HasNoKey();
        }
    }
}
