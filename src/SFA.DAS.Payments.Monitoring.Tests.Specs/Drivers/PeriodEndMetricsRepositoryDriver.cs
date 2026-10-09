using SFA.DAS.Payments.Application.Infrastructure.Logging;
using Moq;
using SFA.DAS.Payments.Model.Core.Entities;
using SFA.DAS.Payments.Monitoring.Metrics.Application.PeriodEnd;
using SFA.DAS.Payments.Monitoring.Metrics.Data;
using SFA.DAS.Payments.Monitoring.Metrics.Model;
using SFA.DAS.Payments.Monitoring.Tests.Specs.Data;
using SFA.DAS.Payments.Monitoring.Tests.Specs.Support;

namespace SFA.DAS.Payments.Monitoring.Tests.Specs.Drivers
{
    public class PeriodEndMetricsRepositoryDriver : IDisposable
    {
        private readonly SqlServerMetricsQueryDataContext dataContext = new SqlServerMetricsQueryDataContext();
        private readonly PeriodEndMetricsRepository repository;

        public PeriodEndMetricsRepositoryDriver()
        {
            var dataContextFactory = new Mock<IMetricsQueryDataContextFactory>();
            dataContextFactory
                .Setup(x => x.Create())
                .Returns(dataContext);

            repository = new PeriodEndMetricsRepository(
                new Mock<IMetricsPersistenceDataContext>().Object,
                dataContextFactory.Object,
                new Mock<IPaymentLogger>().Object);
        }

        public List<ProviderTransactionTypeAmounts> TransactionTypeAmounts { get; private set; } = new List<ProviderTransactionTypeAmounts>();
        public List<ProviderFundingSourceAmounts> FundingSourceAmounts { get; private set; } = new List<ProviderFundingSourceAmounts>();
        public List<ProviderInLearningTotal> InLearningTotals { get; private set; } = new List<ProviderInLearningTotal>();
        public List<ProviderContractTypeAmounts> YearToDatePaymentAmounts { get; private set; } = new List<ProviderContractTypeAmounts>();
        public List<ProviderNegativeEarningsLearnerContractTypeAmounts> LearnerPaymentAmounts { get; private set; } = new List<ProviderNegativeEarningsLearnerContractTypeAmounts>();

        public async Task AddPaymentForPlatform(TransactionType transactionType, CourseType courseType, FundingPlatformType? platform, decimal amount, long? learnerUln = null, byte? collectionPeriod = null)
        {
            var payment = new PaymentBuilder()
                .WithTransactionType(transactionType)
                .WithPlatform(platform)
                .WithCourseType(courseType)
                .WithLearnerUln(learnerUln ?? TestData.LearnerUln)
                .WithCollectionPeriod(collectionPeriod ?? TestData.CollectionPeriod)
                .WithAmount(amount)
                .Build();

            await dataContext.AddAsync(payment);
        }

        //Only works on the repository methods that are used to generate the metrics, not the generation of metrics as a whole
        //Unsure if things should be renamed because of this
        public async Task GenerateMetrics()
        {
            await dataContext.SaveChangesAsync();

            var transactionTypeAmounts = await repository.GetTransactionTypesByContractType(
                TestData.AcademicYear,
                TestData.CollectionPeriod,
                CancellationToken.None);

            var fundingSourceAmounts = await repository.GetFundingSourceAmountsByContractType(
                TestData.AcademicYear,
                TestData.CollectionPeriod,
                CancellationToken.None);

            var inLearningTotals = await repository.GetInLearningCount(
                TestData.AcademicYear,
                TestData.CollectionPeriod,
                CancellationToken.None);

            var yearToDatePaymentAmounts = await repository.GetYearToDatePayments(
                TestData.AcademicYear,
                TestData.YearToDateCollectionPeriod,
                CancellationToken.None);

            // This directly tests the repository platform filter; it doesn't cover the negative earnings discovery logic, which is tested in the unit tests
            var negativeEarningsPaymentAmounts = await repository.GetPaymentAmountsForNegativeEarningsLearnersByContractType(
                new List<long> { TestData.LearnerUln, TestData.DasLearnerUln },
                TestData.AcademicYear,
                CancellationToken.None);

            TransactionTypeAmounts = transactionTypeAmounts
                .Where(x => x.Ukprn == TestData.Ukprn)
                .ToList();
            FundingSourceAmounts = fundingSourceAmounts
                .Where(x => x.Ukprn == TestData.Ukprn)
                .ToList();
            InLearningTotals = inLearningTotals
                .Where(x => x.Ukprn == TestData.Ukprn)
                .ToList();
            YearToDatePaymentAmounts = yearToDatePaymentAmounts
                .Where(x => x.Ukprn == TestData.Ukprn)
                .ToList();
            LearnerPaymentAmounts = negativeEarningsPaymentAmounts
                .Where(x => x.Ukprn == TestData.Ukprn)
                .ToList();
        }

        public Task RemoveTestData()
        {
            return dataContext.DeletePaymentsForUkprn(TestData.Ukprn);
        }

        public void Dispose()
        {
            dataContext.Dispose();
        }
    }
}
