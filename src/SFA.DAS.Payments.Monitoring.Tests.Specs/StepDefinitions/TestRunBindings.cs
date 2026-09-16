using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NServiceBus;
using Reqnroll;
using SFA.DAS.Payments.Application.Infrastructure.Logging;
using SFA.DAS.Payments.Core.Configuration;
using SFA.DAS.Payments.Monitoring.Jobs.Client;
using SFA.DAS.Payments.Monitoring.Jobs.Client.Infrastructure.Messaging;
using SFA.DAS.Payments.Monitoring.Tests.Specs.Messages;

namespace SFA.DAS.Payments.Monitoring.Tests.Specs.StepDefinitions
{
    [Binding]
    public static class TestRunBindings
    {
        public static IEndpointInstance Endpoint { get; private set; }

        public static RecordingMessageSession JobStatusMessageSession { get; private set; }

        [BeforeTestRun]
        public static async Task SetUpEndpoint()
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile(Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json"))
                .AddJsonFile(Path.Combine(Directory.GetCurrentDirectory(), "appsettings.Development.json"), optional: true)
                .Build();
            var configHelper = new JsonConfigurationHelper(configuration);

            JobStatusMessageSession = new RecordingMessageSession();

            var jobMessageClient = new JobMessageClient(
                JobStatusMessageSession,
                new Mock<IPaymentLogger>().Object,
                configHelper);

            var endpointName = "sfa-das-payments-monitoring-specs";
            var endpointConfiguration = new EndpointConfiguration(endpointName);

            endpointConfiguration.RegisterComponents(services =>
            {
                services.AddSingleton<IJobMessageClientFactory>(new SingleClientFactory(jobMessageClient));
                services.AddSingleton<JobStatusIncomingMessageBehaviour>();
                services.AddSingleton<JobStatusOutgoingMessageBehaviour>();
                services.AddSingleton<JobStatusFailedMessageBehaviour>();
            });

            endpointConfiguration.EnableFeature<JobStatusFeature>();
            endpointConfiguration.UseSerialization<NewtonsoftJsonSerializer>();

            var persistence = endpointConfiguration.UsePersistence<AzureTablePersistence>();
            persistence.ConnectionString(configHelper.GetConnectionString("StorageConnectionString"));

            var transport = endpointConfiguration.UseTransport<AzureServiceBusTransport>();
            transport
                .ConnectionString(configHelper.GetConnectionString("MonitoringServiceBusConnectionString"))
                .Transactions(TransportTransactionMode.ReceiveOnly)
                .SubscriptionNamingConvention(ruleName => ruleName.Split('.').LastOrDefault() ?? ruleName);

            transport.Routing().RouteToEndpoint(typeof(TestMonitoredCommand), endpointName);

            endpointConfiguration.Recoverability()
                .Immediate(i => i.NumberOfRetries(0))
                .Delayed(d => d.NumberOfRetries(0));
            endpointConfiguration.SendFailedMessagesTo($"{endpointName}.errors");

            endpointConfiguration.EnableInstallers();

            Endpoint = await NServiceBus.Endpoint.Start(endpointConfiguration);
        }

        [AfterTestRun]
        public static async Task TearDownEndpoint()
        {
            if (Endpoint != null)
            {
                await Endpoint.Stop();
            }
        }

        private class SingleClientFactory : IJobMessageClientFactory
        {
            private readonly IJobMessageClient client;

            public SingleClientFactory(IJobMessageClient client)
            {
                this.client = client;
            }

            public IJobMessageClient Create() => client;
        }
    }
}
