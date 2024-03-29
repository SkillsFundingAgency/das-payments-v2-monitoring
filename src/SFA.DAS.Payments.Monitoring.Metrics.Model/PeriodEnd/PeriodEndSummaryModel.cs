﻿using System;

namespace SFA.DAS.Payments.Monitoring.Metrics.Model.PeriodEnd
{
    public class PeriodEndSummaryModel : IPeriodEndSummaryModel
    {
        public long Id { get; set; }
        public bool IsWithinTolerance { get; set; }
        public short AcademicYear { get; set; }
        public byte CollectionPeriod { get; set; }
        public long JobId { get; set; }
        public decimal Percentage { get; set; }
        public ContractTypeAmountsVerbose PaymentMetrics { get; set; } = new ContractTypeAmountsVerbose();
        public ContractTypeAmounts DcEarnings { get; set; } = new ContractTypeAmounts();
        public ContractTypeAmounts Payments { get; set; } = new ContractTypeAmounts();
        public decimal AdjustedDataLockedEarnings { get; set; }
        public decimal AdjustedDataLockedEarnings16To18 { get; set; }
        public decimal AdjustedDataLockedEarnings19Plus { get; set; }
        public decimal AlreadyPaidDataLockedEarnings { get; set; }
        public decimal AlreadyPaidDataLockedEarnings16To18 { get; set; }
        public decimal AlreadyPaidDataLockedEarnings19Plus { get; set; }
        public decimal TotalDataLockedEarnings { get; set; }
        public decimal TotalDataLockedEarnings16To18 { get; set; }
        public decimal TotalDataLockedEarnings19Plus { get; set; }
        public ContractTypeAmounts HeldBackCompletionPayments { get; set; } = new ContractTypeAmounts();
        public ContractTypeAmounts YearToDatePayments { get; set; } = new ContractTypeAmounts();
        public DataLockTypeCounts DataLockTypeCounts { get; set; } = new DataLockTypeCounts();
        public int? InLearning { get; set; }
        public NegativeEarningsContractTypeAmounts NegativeEarnings { get; set; } = new NegativeEarningsContractTypeAmounts();
    }
}