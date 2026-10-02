using SFA.DAS.Payments.Model.Core;
using SFA.DAS.Payments.Model.Core.Entities;
using SFA.DAS.Payments.Model.Core.Factories;
using SFA.DAS.Payments.Monitoring.Tests.Specs.Support;

namespace SFA.DAS.Payments.Monitoring.Tests.Specs.Data
{
    public class PaymentBuilder
    {
        private TransactionType transactionType = TransactionType.Learning;
        private FundingPlatformType platform = FundingPlatformType.SubmitLearnerData;

        public PaymentBuilder WithTransactionType(TransactionType value)
        {
            transactionType = value;
            return this;
        }

        public PaymentBuilder WithPlatform(FundingPlatformType value)
        {
            platform = value;
            return this;
        }

        public PaymentModel Build()
        {
            return new PaymentModel
            {
                EventId = Guid.NewGuid(),
                EventTime = DateTimeOffset.UtcNow,
                Ukprn = TestData.Ukprn,
                LearnerUln = TestData.LearnerUln,
                CollectionPeriod = CollectionPeriodFactory.CreateFromAcademicYearAndPeriod(TestData.AcademicYear, TestData.CollectionPeriod),
                DeliveryPeriod = TestData.CollectionPeriod,
                LearnerReferenceNumber = "123",
                LearningAimFundingLineType = "Abc",
                LearningAimReference = "lar",
                PriceEpisodeIdentifier = "some-price-episode-identifier",
                ReportingAimFundingLineType = "some-reporting-aim-funding-line-type",
                IlrSubmissionDateTime = DateTime.UtcNow,
                StartDate = DateTime.UtcNow.Date,
                ContractType = ContractType.Act1,
                FundingSource = FundingSourceType.Levy,
                TransactionType = transactionType,
                FundingPlatformType = platform,
                Amount = platform == FundingPlatformType.SubmitLearnerData ? TestData.SldPaymentAmount : TestData.DasPaymentAmount
            };
        }
    }
}
