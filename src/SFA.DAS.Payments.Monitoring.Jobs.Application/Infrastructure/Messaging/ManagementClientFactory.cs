
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.ServiceBus.Management;
using SFA.DAS.Payments.Monitoring.Jobs.Application.Infrastructure.Messaging;

namespace SFA.DAS.Payments.Monitoring.Jobs.Application.Infrastructure.Messaging
{
    public interface IManagementClientFactory
    {
        ManagementClient GetManagementClient();
    }
    
    public class ManagementClientFactory : IManagementClientFactory
    {
        private readonly string serviceBusConnectionString;

        public ManagementClientFactory(string serviceBusConnectionString)
        {
            this.serviceBusConnectionString = serviceBusConnectionString;
        }

        public ManagementClient GetManagementClient()
        {
            return new ManagementClient(serviceBusConnectionString);
        }
    }
}