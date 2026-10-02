using Moq;
using SFA.DAS.Payments.Application.Infrastructure.Logging;
using SFA.DAS.Payments.Model.Core.Entities;
using SFA.DAS.Payments.Monitoring.Metrics.Application.PeriodEnd;
using SFA.DAS.Payments.Monitoring.Metrics.Data;
using SFA.DAS.Payments.Monitoring.Metrics.Model;
using SFA.DAS.Payments.Monitoring.Tests.Specs.Data;
using SFA.DAS.Payments.Monitoring.Tests.Specs.Support;

namespace SFA.DAS.Payments.Monitoring.Tests.Specs.Drivers
{
    public class PeriodEndMetricsDriver : IDisposable
    {
        private readonly SqlServerMetricsQueryDataContext dataContext = new SqlServerMetricsQueryDataContext();
        private readonly PeriodEndMetricsRepository repository;

        public PeriodEndMetricsDriver()
        {
            var dataContextFactory = new Mock<IMetricsQueryDataContextFactory>();
            dataContextFactory.Setup(x => x.Create()).Returns(dataContext);

            repository = new PeriodEndMetricsRepository(
                new Mock<IMetricsPersistenceDataContext>().Object,
                dataContextFactory.Object,
                new Mock<IPaymentLogger>().Object);
        }

        public decimal ExpectedSldTotal { get; private set; }
        public decimal ExcludedDasTotal { get; private set; }
        public decimal MetricsTotal => FundingSourceAmounts.Sum(x => x.Total);
        public List<ProviderFundingSourceAmounts> FundingSourceAmounts { get; private set; } = new List<ProviderFundingSourceAmounts>();

        public async Task AddPaymentsForBothPlatforms(TransactionType transactionType)
        {
            await AddPayment(transactionType, FundingPlatformType.SubmitLearnerData);
            await AddPayment(transactionType, FundingPlatformType.DigitalApprenticeshipService);
        }

        public async Task GenerateMetrics()
        {
            await dataContext.SaveChangesAsync();

            var amounts = await repository.GetFundingSourceAmountsByContractType(
                TestData.AcademicYear,
                TestData.CollectionPeriod,
                CancellationToken.None);

            FundingSourceAmounts = amounts.Where(x => x.Ukprn == TestData.Ukprn).ToList();
        }

        public Task RemoveTestPayments()
        {
            return dataContext.DeletePaymentsForUkprn(TestData.Ukprn);
        }

        public void Dispose()
        {
            dataContext.Dispose();
        }

        private async Task AddPayment(TransactionType transactionType, FundingPlatformType platform)
        {
            var payment = new PaymentBuilder()
                .WithTransactionType(transactionType)
                .WithPlatform(platform)
                .Build();

            await dataContext.AddAsync(payment);

            if (platform == FundingPlatformType.SubmitLearnerData)
                ExpectedSldTotal += payment.Amount;
            else
                ExcludedDasTotal += payment.Amount;
        }
    }
}
