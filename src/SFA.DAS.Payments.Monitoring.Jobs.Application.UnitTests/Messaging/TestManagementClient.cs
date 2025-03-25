using System;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Management;
using Microsoft.Azure.ServiceBus.Primitives;

namespace SFA.DAS.Payments.Monitoring.Jobs.Application.UnitTests.Messaging
{
    public class TestManagementClient : ManagementClient
    {
        private const string ConnectionString = "Endpoint=sb://onebox.windows-int.net/;Authentication=ManagedIdentity"; // test domain name used internally by ManagementClient

        public TestManagementClient() : base(new ServiceBusConnectionStringBuilder(ConnectionString), new TestTokenProvider())
        {
        }

        public TestManagementClient(string connectionString) : base(connectionString)
        {
            
        }

        public TestManagementClient(string endpoint, ITokenProvider tokenProvider) : base(endpoint, tokenProvider)
        {
        }

        public TestManagementClient(ServiceBusConnectionStringBuilder connectionStringBuilder,
            ITokenProvider tokenProvider = null) : base(connectionStringBuilder, tokenProvider)
        {
        }
    }

    public class TestTokenProvider : ITokenProvider
    {
        public async Task<SecurityToken> GetTokenAsync(string appliesTo, TimeSpan timeout)
        {
            return await Task.FromResult(new SecurityToken("token", DateTime.Now.AddDays(1), "audience", "test-token"));
        }
    }
}
