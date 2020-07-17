﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SFA.DAS.Payments.Application.Infrastructure.Logging;
using SFA.DAS.Payments.Model.Core.Entities;
using SFA.DAS.Payments.Monitoring.Metrics.Data;
using SFA.DAS.Payments.Monitoring.Metrics.Model;
using SFA.DAS.Payments.Monitoring.Metrics.Model.PeriodEnd;

namespace SFA.DAS.Payments.Monitoring.Metrics.Application.PeriodEnd
{

    public interface IPeriodEndMetricsRepository
    {
        Task<List<ProviderTransactionTypeAmounts>> GetTransactionTypesByContractType(short academicYear, byte collectionPeriod, CancellationToken cancellationToken);
        Task<List<ProviderFundingSourceAmounts>> GetFundingSourceAmountsByContractType(short academicYear, byte collectionPeriod, CancellationToken cancellationToken);
        Task<List<ProviderTotal>> GetDataLockedEarningsTotals(short academicYear, byte collectionPeriod, CancellationToken cancellationToken);
        Task<List<ProviderTotal>> GetAlreadyPaidDataLockedEarnings(short academicYear, byte collectionPeriod, CancellationToken cancellationToken);
        Task<List<ProviderContractTypeAmounts>> GetHeldBackCompletionPaymentsTotals(short academicYear, byte collectionPeriod, CancellationToken cancellationToken);
        Task<List<ProviderContractTypeAmounts>> GetYearToDatePayments(short academicYear, byte collectionPeriod, CancellationToken cancellationToken);
        Task SaveProviderSummaries(List<ProviderPeriodEndSummaryModel> providerSummaries, PeriodEndSummaryModel overallPeriodEndSummary, CancellationToken cancellationToken);
    }

    public class PeriodEndMetricsRepository : IPeriodEndMetricsRepository
    {
        private readonly IMetricsPersistenceDataContext persistenceDataContext;
        private readonly IMetricsQueryDataContext queryDataContext;
        private readonly IPaymentLogger logger;



        public PeriodEndMetricsRepository( IMetricsPersistenceDataContext persistenceDataContext, IMetricsQueryDataContext queryDataContext, IPaymentLogger logger)
        {
            this.persistenceDataContext = persistenceDataContext ?? throw new ArgumentNullException(nameof(persistenceDataContext));
            this.queryDataContext = queryDataContext ?? throw new ArgumentNullException(nameof(queryDataContext));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }


        public async Task<List<ProviderTransactionTypeAmounts>> GetTransactionTypesByContractType(short academicYear, byte collectionPeriod, CancellationToken cancellationToken)
        {
            var transactionAmounts =  await queryDataContext.Payments.Where(x=>x.CollectionPeriod.AcademicYear == academicYear && x.CollectionPeriod.Period == collectionPeriod)
                .GroupBy(p => new { p.Ukprn, p.ContractType, p.TransactionType })
                .Select(group => new
                {
                    UkPrn = group.Key.Ukprn,
                    ContractType = group.Key.ContractType,
                    TransactionType = group.Key.TransactionType,
                    Amount = group.Sum(x => x.Amount)
                })
                .ToListAsync(cancellationToken);

            return transactionAmounts
                .GroupBy(x => new {x.UkPrn, x.ContractType})
                .Select(group => new ProviderTransactionTypeAmounts
                {
                    Ukprn = group.Key.UkPrn,
                    ContractType = group.Key.ContractType,
                    TransactionType1 = group.Where(x => x.TransactionType == TransactionType.Learning).Sum(x => (decimal?)x.Amount) ?? 0,
                    TransactionType2 = group.Where(x => x.TransactionType == TransactionType.Completion).Sum(x => (decimal?)x.Amount) ?? 0,
                    TransactionType3 = group.Where(x => x.TransactionType == TransactionType.Balancing).Sum(x => (decimal?)x.Amount) ?? 0,
                    TransactionType4 = group.Where(x => x.TransactionType == TransactionType.First16To18EmployerIncentive).Sum(x => (decimal?)x.Amount) ?? 0,
                    TransactionType5 = group.Where(x => x.TransactionType == TransactionType.First16To18ProviderIncentive).Sum(x => (decimal?)x.Amount) ?? 0,
                    TransactionType6 = group.Where(x => x.TransactionType == TransactionType.Second16To18EmployerIncentive).Sum(x => (decimal?)x.Amount) ?? 0,
                    TransactionType7 = group.Where(x => x.TransactionType == TransactionType.Second16To18ProviderIncentive).Sum(x => (decimal?)x.Amount) ?? 0,
                    TransactionType8 = group.Where(x => x.TransactionType == TransactionType.OnProgramme16To18FrameworkUplift).Sum(x => (decimal?)x.Amount) ?? 0,
                    TransactionType9 = group.Where(x => x.TransactionType == TransactionType.Completion16To18FrameworkUplift).Sum(x => (decimal?)x.Amount) ?? 0,
                    TransactionType10 = group.Where(x => x.TransactionType == TransactionType.Balancing16To18FrameworkUplift).Sum(x => (decimal?)x.Amount) ?? 0,
                    TransactionType11 = group.Where(x => x.TransactionType == TransactionType.FirstDisadvantagePayment).Sum(x => (decimal?)x.Amount) ?? 0,
                    TransactionType12 = group.Where(x => x.TransactionType == TransactionType.SecondDisadvantagePayment).Sum(x => (decimal?)x.Amount) ?? 0,
                    TransactionType13 = group.Where(x => x.TransactionType == TransactionType.OnProgrammeMathsAndEnglish).Sum(x => (decimal?)x.Amount) ?? 0,
                    TransactionType14 = group.Where(x => x.TransactionType == TransactionType.BalancingMathsAndEnglish).Sum(x => (decimal?)x.Amount) ?? 0,
                    TransactionType15 = group.Where(x => x.TransactionType == TransactionType.LearningSupport).Sum(x => (decimal?)x.Amount) ?? 0,
                    TransactionType16 = group.Where(x => x.TransactionType == TransactionType.CareLeaverApprenticePayment).Sum(x => (decimal?)x.Amount) ?? 0,
                })
                .ToList();
        }

        public async Task<List<ProviderFundingSourceAmounts>> GetFundingSourceAmountsByContractType(short academicYear, byte collectionPeriod,
            CancellationToken cancellationToken)
        {
             var transactionAmounts =  await queryDataContext.Payments.Where(x=>x.CollectionPeriod.AcademicYear == academicYear && x.CollectionPeriod.Period == collectionPeriod)
                .GroupBy(p => new { p.Ukprn, p.ContractType, p.FundingSource })
                .Select(group => new
                {
                    UkPrn = group.Key.Ukprn,
                    ContractType = group.Key.ContractType,
                    FundingSource = group.Key.FundingSource,
                    Amount = group.Sum(x => x.Amount)
                })
                .ToListAsync(cancellationToken);

            return transactionAmounts
                .GroupBy(x => new {x.UkPrn, x.ContractType})
                .Select(group => new ProviderFundingSourceAmounts
                {
                    Ukprn = group.Key.UkPrn,
                    ContractType = group.Key.ContractType,
                    FundingSource1 = group.Where(x => x.FundingSource == FundingSourceType.Levy).Sum(x => (decimal?)x.Amount) ?? 0,
                    FundingSource2 = group.Where(x => x.FundingSource == FundingSourceType.CoInvestedSfa).Sum(x => (decimal?)x.Amount) ?? 0,
                    FundingSource3 = group.Where(x => x.FundingSource == FundingSourceType.CoInvestedEmployer).Sum(x => (decimal?)x.Amount) ?? 0,
                    FundingSource4 = group.Where(x => x.FundingSource == FundingSourceType.FullyFundedSfa).Sum(x => (decimal?)x.Amount) ?? 0,
                    FundingSource5 = group.Where(x => x.FundingSource == FundingSourceType.Transfer).Sum(x => (decimal?)x.Amount) ?? 0,
                })
                .ToList();
        }

        public Task<List<ProviderTotal>> GetDataLockedEarningsTotals(short academicYear, byte collectionPeriod, CancellationToken cancellationToken)
        {
            return queryDataContext.GetDataLockedEarningsTotals(academicYear, collectionPeriod, cancellationToken);
        }

        public async Task<List<ProviderTotal>> GetAlreadyPaidDataLockedEarnings(short academicYear, byte collectionPeriod, CancellationToken cancellationToken)
        {
            return await queryDataContext.GetAlreadyPaidDataLockProviderTotals(cancellationToken);
        }

        public async Task<List<ProviderContractTypeAmounts>> GetHeldBackCompletionPaymentsTotals(short academicYear, byte collectionPeriod,
            CancellationToken cancellationToken)
        {
           return  await queryDataContext.GetHeldBackCompletionPaymentTotals(academicYear, collectionPeriod, cancellationToken);
        }

        public async Task<List<ProviderContractTypeAmounts>> GetYearToDatePayments(short academicYear, byte collectionPeriod, CancellationToken cancellationToken)
        {
            var amounts = await queryDataContext.Payments
                .AsNoTracking()
                .Where(p => p.CollectionPeriod.AcademicYear == academicYear &&
                            p.CollectionPeriod.Period < collectionPeriod)
                .GroupBy(p => new {p.Ukprn, p.ContractType})
                .Select(g => new { Ukprn = g.Key.Ukprn, ContractType = g.Key.ContractType, Amount = g.Sum(p => p.Amount) })
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return amounts.Select(group => new ProviderContractTypeAmounts()
            {
                Ukprn =  group.Ukprn,
                ContractType1 = amounts.FirstOrDefault(amount => amount.ContractType == ContractType.Act1)?.Amount ?? 0,
                ContractType2 = amounts.FirstOrDefault(amount => amount.ContractType == ContractType.Act2)?.Amount ?? 0,
            }).ToList();

        }

        public async Task SaveProviderSummaries(List<ProviderPeriodEndSummaryModel> providerSummaries, PeriodEndSummaryModel overallPeriodEndSummary,
            CancellationToken cancellationToken)
        {
            await persistenceDataContext.SaveProviderSummaries(providerSummaries, overallPeriodEndSummary,
                cancellationToken);
        }
    }
}