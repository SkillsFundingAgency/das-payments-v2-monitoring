using Microsoft.EntityFrameworkCore;

namespace SFA.DAS.Payments.Monitoring.Metrics.Model
{
    [Keyless]
    public class ProviderFundingLineTypeAmounts
    {
        public long Ukprn { get; set; }
        public decimal FundingLineType16To18Amount { get; set; }
        public decimal FundingLineType19PlusAmount { get; set; }
        public decimal Total { get; set; }
    }
}