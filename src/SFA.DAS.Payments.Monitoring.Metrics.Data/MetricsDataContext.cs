﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SFA.DAS.Payments.Monitoring.Metrics.Data.Configuration;
using SFA.DAS.Payments.Monitoring.Metrics.Model.Submission;

namespace SFA.DAS.Payments.Monitoring.Metrics.Data
{
    public interface IMetricsDataContext
    {
        DbSet<SubmissionSummaryModel> SubmissionSummaries { get; }
        DbSet<DataLockedEarningsModel> DataLockedEarnings { get; }
        DbSet<EarningsModel> Earnings { get; }
        DbSet<RequiredPaymentsModel> RequiredPayments { get; }
        Task Save(SubmissionSummaryModel submissionSummary, CancellationToken cancellationToken);
    }

    public class MetricsDataContext : DbContext, IMetricsDataContext
    {
        private readonly string connectionString;

        public virtual DbSet<SubmissionSummaryModel> SubmissionSummaries { get; set; }
        public virtual DbSet<DataLockedEarningsModel> DataLockedEarnings { get; set; }
        public virtual DbSet<EarningsModel> Earnings { get; set; }
        public virtual DbSet<RequiredPaymentsModel> RequiredPayments { get; set; }

        public MetricsDataContext(string connectionString)
        {
            this.connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.HasDefaultSchema("Metrics");
            modelBuilder.ApplyConfiguration(new SubmissionSummaryModelConfiguration());
            modelBuilder.ApplyConfiguration(new DataLockedEarningsModelConfiguration());
            modelBuilder.ApplyConfiguration(new EarningsModelConfiguration());
            modelBuilder.ApplyConfiguration(new RequiredPaymentsModelConfiguration());
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(connectionString);
        }

        public async Task Save(SubmissionSummaryModel submissionSummary, CancellationToken cancellationToken)
        {
            var transaction = await Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await Database.ExecuteSqlCommandAsync($@"
                    Delete 
                        From [Metrics].[SubmissionSummary] 
                    Where 
                        Ukprn = {submissionSummary.Ukprn}
                        And AcademicYear = {submissionSummary.AcademicYear}
                        And CollectionPeriod = {submissionSummary.CollectionPeriod}
                    "
                    , cancellationToken);
                SubmissionSummaries.Add(submissionSummary);
                await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                transaction.Commit();
            }
            catch 
            {
                transaction.Rollback();
                throw;
            }
        }
    }
}