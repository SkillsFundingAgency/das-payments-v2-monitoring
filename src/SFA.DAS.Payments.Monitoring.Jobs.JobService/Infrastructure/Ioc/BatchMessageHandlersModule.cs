﻿using Autofac;
using SFA.DAS.Payments.Application.Infrastructure.Ioc;
using SFA.DAS.Payments.Application.Infrastructure.Logging;
using SFA.DAS.Payments.Application.Messaging;
using SFA.DAS.Payments.Core.Configuration;
using SFA.DAS.Payments.Monitoring.Jobs.JobService.Handlers;
using SFA.DAS.Payments.Monitoring.Jobs.JobService.Handlers.PeriodEnd;
using SFA.DAS.Payments.Monitoring.Jobs.JobService.Handlers.Submission;
using SFA.DAS.Payments.Monitoring.Jobs.Messages.Commands;
using SFA.DAS.Payments.ServiceFabric.Core;
using NServiceBus;

namespace SFA.DAS.Payments.Monitoring.Jobs.JobService.Infrastructure.Ioc
{
    public class BatchMessageHandlersModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.Register(c =>
                {
                    var appConfig = c.Resolve<IApplicationConfiguration>();
                    var configHelper = c.Resolve<IConfigurationHelper>();
                    return new ServiceBusBatchCommunicationListener(configHelper.GetConnectionString("MonitoringServiceBusConnectionString"),
                        appConfig.EndpointName,
                        appConfig.FailedMessagesQueue,
                        c.Resolve<IPaymentLogger>(),
                        c.Resolve<IContainerScopeFactory>());
                })
                .As<IServiceBusBatchCommunicationListener>()
                .SingleInstance();

            builder.RegisterType<RecordJobMessageProcessingStatusBatchHandler>()
                .As<IHandleMessageBatches<RecordJobMessageProcessingStatus>>()
                .InstancePerLifetimeScope();

            builder.RegisterType<RecordEarningsJobBatchHandler>()
                .As<IHandleMessageBatches<RecordEarningsJob>>()
                .InstancePerLifetimeScope();

            builder.RegisterType<RecordJobAdditionalMessageBatchHandler>()
                .As<IHandleMessageBatches<RecordJobAdditionalMessages>>()
                .InstancePerLifetimeScope();

            builder.RegisterType<RecordPeriodEndStopJobHandler>()
                .As<IHandleMessageBatches<RecordPeriodEndStopJob>>()
                .InstancePerLifetimeScope();

            builder.RegisterType<RecordPeriodEndRunJobHandler>()
                .As<IHandleMessageBatches<RecordPeriodEndRunJob>>()
                .InstancePerLifetimeScope();

            builder.RegisterType<RecordPeriodEndStartJobHandler>()
                .As<IHandleMessageBatches<RecordPeriodEndStartJob>>()
                .InstancePerLifetimeScope();

            builder.RegisterType<RecordPeriodEndIlrReprocessingStartedJobHandler>()
                .As<IHandleMessageBatches<RecordPeriodEndIlrReprocessingStartedJob>>()
                .InstancePerLifetimeScope();

            builder.RegisterType<RecordPeriodEndSubmissionWindowValidationJobHandler>()
                .As<IHandleMessageBatches<RecordPeriodEndSubmissionWindowValidationJob>>()
                .InstancePerLifetimeScope();

            builder.RegisterType<RecordSubmissionSucceededHandler>()
                .As<IHandleMessageBatches<RecordEarningsJobSucceeded>>()
                .InstancePerLifetimeScope();

            builder.RegisterType<RecordSubmissionFailedHandler>()
                .As<IHandleMessageBatches<RecordEarningsJobFailed>>()
                .InstancePerLifetimeScope();

            builder.RegisterType<RecordPeriodEndRequestReportsJobHandler>()
                .As<IHandleMessageBatches<RecordPeriodEndRequestReportsJob>>()
                .InstancePerLifetimeScope();

            builder.RegisterType<RecordPeriodEndFcsHandOverCompleteJobHandler>()
                .As<IHandleMessageBatches<RecordPeriodEndFcsHandOverCompleteJob>>()
                .InstancePerLifetimeScope();
            builder.Register(c =>
                {
                    var appConfig = c.Resolve<IApplicationConfiguration>();
                    return new EndpointConfiguration(appConfig.EndpointName);
                }).As<EndpointConfiguration>()
                .SingleInstance();
        }
    }
}