using Microsoft.Extensions.Configuration;

namespace SFA.DAS.Payments.Monitoring.Tests.Specs.Configuration
{
    public static class TestConfiguration
    {
        private static readonly IConfigurationRoot Configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        public static string PaymentsConnectionString
        {
            get
            {
                var value = Configuration["PaymentsConnectionString"];

                if (string.IsNullOrWhiteSpace(value))
                    value = Configuration.GetConnectionString("PaymentsConnectionString");

                if (string.IsNullOrWhiteSpace(value))
                    throw new InvalidOperationException("PaymentsConnectionString is not configured. Set it in appsettings.development.json or the PaymentsConnectionString environment variable.");

                return value;
            }
        }
    }
}
