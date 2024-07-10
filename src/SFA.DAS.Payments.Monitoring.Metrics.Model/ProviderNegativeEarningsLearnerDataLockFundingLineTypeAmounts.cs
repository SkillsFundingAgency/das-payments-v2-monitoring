using Microsoft.EntityFrameworkCore;

namespace SFA.DAS.Payments.Monitoring.Metrics.Model
{
    [Keyless]
    public class ProviderNegativeEarningsLearnerDataLockFundingLineTypeAmounts
    {
        public long Ukprn { get; set; }
        public long LearnerUln { get; set; }
        public decimal FundingLineType16To18Amount { get; set; }
        public decimal FundingLineType19PlusAmount { get; set; }
        public decimal Total { get; set; }
    }
}