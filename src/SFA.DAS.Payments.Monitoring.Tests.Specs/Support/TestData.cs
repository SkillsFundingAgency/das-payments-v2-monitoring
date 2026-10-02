using SFA.DAS.Payments.Model.Core.Entities;

namespace SFA.DAS.Payments.Monitoring.Tests.Specs.Support
{
    public static class TestData
    {
        public const long Ukprn = 99999901;
        public const long LearnerUln = 9999990001;
        public const short AcademicYear = 2526;
        public const byte CollectionPeriod = 1;
        public const decimal SldPaymentAmount = 1000m;
        public const decimal DasPaymentAmount = 500m;
        public const TransactionType ShortCourseMilestone = (TransactionType)17;
    }
}
