﻿using System.Linq;
using System.Threading.Tasks;
using Autofac;
using NServiceBus;
using NServiceBus.Faults;
using NServiceBus.Features;
using SFA.DAS.Payments.Application.Infrastructure.Logging;
using SFA.DAS.Payments.Core.Configuration;
using SFA.DAS.Payments.Monitoring.Jobs.Client.Infrastructure.Messaging;
using SFA.DAS.Payments.Monitoring.Jobs.Data;
using SFA.DAS.Payments.Monitoring.Jobs.Messages.Commands;

namespace SFA.DAS.Payments.Monitoring.Jobs.Client.Infrastructure.Ioc
{
    public class JobStatusClientModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {

            builder.Register((c, p) =>
            {
                var configHelper = c.Resolve<IConfigurationHelper>();
                return new JobsDataContext(configHelper.GetConnectionString("PaymentsConnectionString"));
            })
                .As<IJobsDataContext>()
                .InstancePerDependency();

            builder.Register((c, p) =>
            {
                var logger = c.Resolve<IPaymentLogger>();
                var endpointConfig = CreateEndpointConfiguration(c, logger);
                return new MonitoringMessageSessionFactory(endpointConfig);
            })
                .As<IMonitoringMessageSessionFactory>()
                .SingleInstance();

            builder.Register((c, p) =>
            {
                var logger = c.Resolve<IPaymentLogger>();
                var config = c.Resolve<IConfigurationHelper>();
                var factory = c.Resolve<IMonitoringMessageSessionFactory>();
                var dataContext = c.Resolve<IJobsDataContext>();
                //                    return new EarningsJobClient(logger, dataContext, c.Resolve<Application.Infrastructure.Telemetry.ITelemetry>());
                return new EarningsJobClient(factory.Create(), logger, config);
            })
                .As<IEarningsJobClient>()
                .InstancePerDependency();

            builder.RegisterType<EarningsJobClientFactory>()
                .As<IEarningsJobClientFactory>()
                .SingleInstance();

            builder.RegisterType<PeriodEndJobClient>()
                .As<IPeriodEndJobClient>()
                .SingleInstance();

            builder.Register((c, p) =>
            {
                var config = c.Resolve<IConfigurationHelper>();
                var logger = c.Resolve<IPaymentLogger>();
                var factory = c.Resolve<IMonitoringMessageSessionFactory>();
                return new JobMessageClient(factory.Create(), logger, config);
            })
                .As<IJobMessageClient>()
                .SingleInstance();

            builder.RegisterType<JobMessageClientFactory>()
                .As<IJobMessageClientFactory>()
                .SingleInstance();

            builder.RegisterType<JobStatusIncomingMessageBehaviour>()
                .SingleInstance();

            builder.RegisterType<JobStatusOutgoingMessageBehaviour>()
                .SingleInstance();
            
            builder.Register(c =>
                {
                    var appConfig = c.Resolve<IApplicationConfiguration>();
                    return new EndpointConfiguration(appConfig.EndpointName);
                }).As<EndpointConfiguration>()
                .SingleInstance();

            //builder.RegisterBuildCallback(c =>
            //{
            //    var endpointConfig = c.Resolve<EndpointConfiguration>();
            //    //endpointConfig.Pipeline.Register(typeof(JobStatusIncomingMessageBehaviour),
            //    //    "Job Status Incoming message behaviour");
            //    //endpointConfig.Pipeline.Register(typeof(JobStatusOutgoingMessageBehaviour),
            //    //    "Job Status Outgoing message behaviour");

            //    endpointConfig.Recoverability().Failed(
            //        failedMessage =>
            //        {
            //            failedMessage.OnMessageSentToErrorQueue((message, token) =>
            //            {
            //                var factory = c.Resolve<IJobMessageClientFactory>();
            //                var client = factory.Create();
            //                client.ProcessingFailedForJobMessage(message.Body.ToArray()).Wait(2000);
            //                return Task.CompletedTask;
            //            });
            //        }
            //    );
            //});
        }

        private EndpointConfiguration CreateEndpointConfiguration(IComponentContext container, IPaymentLogger logger)
        {
            var config = container.Resolve<IApplicationConfiguration>();
            var configHelper = container.Resolve<IConfigurationHelper>();
            var endpointConfiguration = new EndpointConfiguration(config.EndpointName);
            var jobsEndpointName = configHelper.GetSettingOrDefault("Monitoring_JobsService_EndpointName", "sfa-das-payments-monitoring-jobs");

            var conventions = endpointConfiguration.Conventions();
            conventions
                .DefiningCommandsAs(t => t.IsAssignableTo<JobsCommand>());

            var persistence = endpointConfiguration.UsePersistence<AzureTablePersistence>();
            persistence.ConnectionString(config.StorageConnectionString);

            var transport = endpointConfiguration.UseTransport<AzureServiceBusTransport>();
            transport
                .ConnectionString(configHelper.GetConnectionString("MonitoringServiceBusConnectionString"))
                .Transactions(TransportTransactionMode.ReceiveOnly)
                .SubscriptionNamingConvention(ruleName => ruleName.Split('.').LastOrDefault() ?? ruleName);

            transport.Routing().RouteToEndpoint(typeof(RecordEarningsJob).Assembly, jobsEndpointName);
            endpointConfiguration.SendFailedMessagesTo(config.FailedMessagesQueue);
            endpointConfiguration.UseSerialization<NewtonsoftJsonSerializer>();
            endpointConfiguration.EnableInstallers();

            endpointConfiguration.RegisterComponents(cfg => cfg.RegisterSingleton(logger));
            endpointConfiguration.SendOnly();
            return endpointConfiguration;
        }
    }
}