using Microsoft.Extensions.Configuration;
using SFA.DAS.Payments.Core.Configuration;

namespace SFA.DAS.Payments.Monitoring.Tests.Specs.StepDefinitions
{
    public class JsonConfigurationHelper : IConfigurationHelper
    {
        private readonly IConfiguration configuration;

        public JsonConfigurationHelper(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public bool HasSetting(string sectionName, string settingName) => GetSetting(sectionName, settingName) != null;

        public string GetSetting(string sectionName, string settingName)
            => configuration.GetConnectionString(settingName) ?? configuration[settingName];
    }
}
