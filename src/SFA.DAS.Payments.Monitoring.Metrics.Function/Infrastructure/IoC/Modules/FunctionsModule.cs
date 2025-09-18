using System;
using System.Configuration;
using Autofac;
using Microsoft.EntityFrameworkCore;
using SFA.DAS.Payments.Core.Configuration;
using SFA.DAS.Payments.Monitoring.Metrics.Application.PeriodEnd;
using SFA.DAS.Payments.Monitoring.Metrics.Application.Submission;
using SFA.DAS.Payments.Monitoring.Metrics.Data;
using SFA.DAS.Payments.Monitoring.Metrics.Domain.Submission;
using SFA.DAS.Payments.Monitoring.Metrics.Function.Infrastructure.Configuration;

namespace SFA.DAS.Payments.Monitoring.Metrics.Function.Infrastructure.IoC.Modules
{
    public class FunctionsModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<SubmissionWindowValidationService>().As<ISubmissionWindowValidationService>().InstancePerLifetimeScope();
            builder.RegisterType<SubmissionMetricsRepository>().As<ISubmissionMetricsRepository>().InstancePerLifetimeScope();
            builder.RegisterType<SubmissionsSummary>().As<ISubmissionsSummary>().InstancePerLifetimeScope();

            builder.Register((c, p) =>
                {
                    var config = c.Resolve<ISubmissionMetricsConfiguration>();
                    var dbContextOptions = new DbContextOptionsBuilder()
                        .UseSqlServer(config.PaymentsConnectionString, 
                  optionsBuilder => optionsBuilder.CommandTimeout(270)).Options;
                    return new MetricsPersistenceDataContext(dbContextOptions);
                })
                .As<IMetricsPersistenceDataContext>()
                .InstancePerLifetimeScope();

            //NOTE: Not in use but required to get SubmissionMetricsRepository working
            builder.RegisterType<MetricsQueryDataContextFactory>()
                .As<IMetricsQueryDataContextFactory>()
                .InstancePerLifetimeScope();

            builder.RegisterType<SubmissionJobsService>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<SubmissionJobsRepository>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.Register((c, p) =>
                {
                    var config = c.Resolve<ISubmissionMetricsConfiguration>();
                    var dbContextOptions = new DbContextOptionsBuilder()
                        .UseSqlServer(config.PaymentsConnectionString, 
                            optionsBuilder => optionsBuilder.CommandTimeout(270)).Options;
                    return new SubmissionJobsDataContext(dbContextOptions);
                })
                .As<ISubmissionJobsDataContext>()
                .InstancePerLifetimeScope();

            builder.RegisterType<PeriodEndMetricsService>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<PeriodEndSummaryFactory>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<DcMetricsDataContextFactory>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<PeriodEndMetricsRepository>().AsImplementedInterfaces().InstancePerLifetimeScope();

            builder.Register((c, p) =>
                {
                    var configHelper = c.Resolve<IConfigurationHelper>();

                    var dbContextOptions = new DbContextOptionsBuilder().UseSqlServer(
                        configHelper.GetConnectionString("DcEarnings2526ConnectionString"),
                        optionsBuilder => optionsBuilder.CommandTimeout(270)).Options;

                    return new DcMetricsDataContext(dbContextOptions);
                })
                .Named<IDcMetricsDataContext>("DcEarnings2526DataContext")
                .InstancePerDependency();

            builder.Register((c, p) =>
                {
                    var configHelper = c.Resolve<IConfigurationHelper>();

                    var dbContextOptions = new DbContextOptionsBuilder().UseSqlServer(
                        configHelper.GetConnectionString("DcEarnings2425ConnectionString"),
                        optionsBuilder => optionsBuilder.CommandTimeout(270)).Options;

                    return new DcMetricsDataContext(dbContextOptions);
                })
                .Named<IDcMetricsDataContext>("DcEarnings2425DataContext")
                .InstancePerDependency();

            builder.Register((c, p) =>
                {
                    var configHelper = c.Resolve<IConfigurationHelper>();
                    var dbContextOptions = new DbContextOptionsBuilder()
                        .UseSqlServer(
                            configHelper.GetConnectionString("PaymentsMetricsConnectionString"),
                            sqlOptions =>
                            {
                                sqlOptions.CommandTimeout(270);
                                sqlOptions.EnableRetryOnFailure(
                                    // consider making these configurable if it has the desired effect
                                    maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(10), errorNumbersToAdd: null);

                            }).Options;
                    return new MetricsQueryDataContext(dbContextOptions);
                })
                .As<IMetricsQueryDataContext>()
                .InstancePerDependency();
        }
    }
}
