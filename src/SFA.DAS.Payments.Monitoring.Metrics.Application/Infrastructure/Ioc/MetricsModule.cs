﻿using Autofac;
using Microsoft.EntityFrameworkCore;
using SFA.DAS.Payments.Core.Configuration;
using SFA.DAS.Payments.Monitoring.Metrics.Application.Submission;
using SFA.DAS.Payments.Monitoring.Metrics.Data;

namespace SFA.DAS.Payments.Monitoring.Metrics.Application.Infrastructure.Ioc
{
    public class MetricsModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<SubmissionSummaryFactory>()
                .As<ISubmissionSummaryFactory>()
                .SingleInstance();

            builder.RegisterType<SubmissionMetricsService>()
                .As<ISubmissionMetricsService>()
                .InstancePerLifetimeScope();

            builder.Register((c, p) =>
            {
                var configHelper = c.Resolve<IConfigurationHelper>();

                var dbContextOptions = new DbContextOptionsBuilder().UseSqlServer(
                    configHelper.GetConnectionString("DcEarnings2223ConnectionString"),
                    optionsBuilder => optionsBuilder.CommandTimeout(270)).Options;

                return new DcMetricsDataContext(dbContextOptions);
            })
                .Named<IDcMetricsDataContext>("DcEarnings2223DataContext")
                .InstancePerLifetimeScope();

            builder.Register((c, p) =>
            {
                var configHelper = c.Resolve<IConfigurationHelper>();

                var dbContextOptions = new DbContextOptionsBuilder().UseSqlServer(
                    configHelper.GetConnectionString("DcEarnings2122ConnectionString"),
                    optionsBuilder => optionsBuilder.CommandTimeout(270)).Options;

                return new DcMetricsDataContext(dbContextOptions);
            })
                .Named<IDcMetricsDataContext>("DcEarnings2122DataContext")
                .InstancePerLifetimeScope();

            builder.RegisterType<DcMetricsDataContextFactory>()
                .As<IDcMetricsDataContextFactory>()
                .InstancePerLifetimeScope();

            builder.RegisterType<MetricsQueryDataContextFactory>()
                .As<IMetricsQueryDataContextFactory>()
                .InstancePerLifetimeScope();

            builder.Register((c, p) =>
            {
                var configHelper = c.Resolve<IConfigurationHelper>();
                var dbContextOptions = new DbContextOptionsBuilder().UseSqlServer(
                    configHelper.GetConnectionString("PaymentsMetricsConnectionString"),
                    optionsBuilder => optionsBuilder.CommandTimeout(270)).Options;
                return new MetricsQueryDataContext(dbContextOptions);
            })
                .As<IMetricsQueryDataContext>()
                .InstancePerDependency();

            builder.Register((c, p) =>
            {
                var configHelper = c.Resolve<IConfigurationHelper>();
                var dbContextOptions = new DbContextOptionsBuilder().UseSqlServer(
                    configHelper.GetConnectionString("PaymentsConnectionString"),
                    optionsBuilder => optionsBuilder.CommandTimeout(270)).Options;
                return new MetricsPersistenceDataContext(dbContextOptions);
            })
                .As<IMetricsPersistenceDataContext>()
                .InstancePerLifetimeScope();

            builder.RegisterType<SubmissionMetricsRepository>()
                .As<ISubmissionMetricsRepository>()
                .InstancePerLifetimeScope();

            builder.Register((c, p) =>
                {
                    var configHelper = c.Resolve<IConfigurationHelper>();

                    var dbContextOptions = new DbContextOptionsBuilder()
                        .UseSqlServer(configHelper.GetConnectionString("PaymentsConnectionString"),
                            optionsBuilder => optionsBuilder.CommandTimeout(270)).Options;
                    return new SubmissionJobsDataContext(dbContextOptions);
                })
                .As<ISubmissionJobsDataContext>()
                .InstancePerLifetimeScope();

            builder
                .RegisterType<SubmissionJobsRepository>()
                .AsImplementedInterfaces()
                .InstancePerLifetimeScope();
        }
    }
}