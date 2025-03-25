using Azure.Messaging.ServiceBus;

namespace SFA.DAS.Payments.Monitoring.Jobs.Application.Infrastructure.Messaging
{
    public interface IServiceBusClientFactory
    {
        ServiceBusClient GetServiceBusClient();
    }

    public class ServiceBusClientFactory : IServiceBusClientFactory
    {
        private readonly string serviceBusConnectionString;

        public ServiceBusClientFactory(string serviceBusConnectionString)
        {
            this.serviceBusConnectionString = serviceBusConnectionString;
        }
        
        public ServiceBusClient GetServiceBusClient()
        {
            return new ServiceBusClient(serviceBusConnectionString);
        }
    }
}
