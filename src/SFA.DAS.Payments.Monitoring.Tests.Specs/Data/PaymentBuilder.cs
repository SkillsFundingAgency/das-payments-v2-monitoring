using SFA.DAS.Payments.Model.Core;
using SFA.DAS.Payments.Model.Core.Entities;
using SFA.DAS.Payments.Model.Core.Factories;
using SFA.DAS.Payments.Monitoring.Tests.Specs.Support;

namespace SFA.DAS.Payments.Monitoring.Tests.Specs.Data
{
    public class PaymentBuilder
    {
        private TransactionType transactionType = TransactionType.Learning;
        private FundingPlatformType? platform = FundingPlatformType.SubmitLearnerData;
        private CourseType courseType = CourseType.Apprenticeship;
        private long learnerUln = TestData.LearnerUln;
        private byte collectionPeriod = TestData.CollectionPeriod;
        private decimal amount;

        public PaymentBuilder WithTransactionType(TransactionType value)
        {
            transactionType = value;
            return this;
        }

        public PaymentBuilder WithCourseType(CourseType value)
        {
            courseType = value;
            return this;
        }

        public PaymentBuilder WithPlatform(FundingPlatformType? value)
        {
            platform = value;
            return this;
        }

        public PaymentBuilder WithAmount(decimal value)
        {
            amount = value;
            return this;
        }

        public PaymentBuilder WithLearnerUln(long value)
        {
            learnerUln = value;
            return this;
        }

        public PaymentBuilder WithCollectionPeriod(byte value)
        {
            collectionPeriod = value;
            return this;
        }

        public PaymentModel Build()
        {
            return new PaymentModel
            {
                EventId = Guid.NewGuid(),
                EventTime = DateTimeOffset.UtcNow,
                Ukprn = TestData.Ukprn,
                LearnerUln = learnerUln,
                CollectionPeriod = CollectionPeriodFactory.CreateFromAcademicYearAndPeriod(TestData.AcademicYear, collectionPeriod),
                DeliveryPeriod = collectionPeriod,
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
                CourseType = courseType,
                Amount = amount,
            };
        }
    }
}
