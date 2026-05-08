using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SFA.DAS.Payments.Monitoring.Alerts.Function.Helpers;
using SFA.DAS.Payments.Monitoring.Alerts.Function.JsonHelpers;
using SFA.DAS.Payments.Monitoring.Alerts.Function.Models;
using SFA.DAS.Payments.Monitoring.Alerts.Function.TypedClients;

namespace SFA.DAS.Payments.Monitoring.Alerts.Function.Services
{
    public class TeamsService : ITeamsService
    {
        private readonly IAppInsightsClient _appInsightsClient;
        private readonly ISlackAlertHelper _slackAlertHelper;
        private readonly ITeamsClient _teamsClient;
        private readonly IDynamicJsonDeserializer _deserializer;
        private ILogger _logger;

        public TeamsService(IDynamicJsonDeserializer deserializer,
                            ISlackAlertHelper slackAlertHelper,
                            ITeamsClient teamsClient,
                            IAppInsightsClient appInsightsClient)
        {
            _deserializer = deserializer;
            alertHelper = alertHelper;
            _appInsightsClient = appInsightsClient;
            _teamsClient = teamsClient;
        }

        public async Task PostTeamsAlert(ILogger logger, string appInsightsAlertPayload, string teamsWebhookUrl)
        {
            _logger = logger;

            dynamic alert = _deserializer.Deserialize(appInsightsAlertPayload);

            string searchResultApiUrl = alert.data.alertContext.condition.allOf[0].linkToSearchResultsAPI;
            alert.data.alertContext.SearchResults = await _appInsightsClient.GetSearchResultsAsync(searchResultApiUrl);

            var severity = alert.data.essentials.severity;
            

            var appInsightsSearchResultsUiLink = alert.data.alertContext.condition.allOf[0].linkToSearchResultsUI;

            foreach (var table in alert.data.alertContext.SearchResults.tables)
            {
                foreach (var row in table.rows)
                {
                    var customDimensions = JsonSerializer.Deserialize<Dictionary<string, string>>(row[3]);
                    var customMeasurements = JsonSerializer.Deserialize<Dictionary<string, double>>(row[4]);
                    DateTime timestamp = DateTime.Parse(row[0]);
                    Dictionary<string,string> alertVariables = _alertHelper.ExtractAlertVariables(customMeasurements, customDimensions, timestamp);
                    string alertDescription = alert.data.essentials.description;
                    await PostTeamsAlert(alertVariables, slackChannelUri, alertDescription, appInsightsSearchResultsUiLink, timestamp);
                }
            }
        }

        private async Task PostTeamsAlert(Dictionary<string, string> alertVariables,
                                          string ChannelUri,
                                          string alertDescription,
                                          string appInsightsSearchResultsUiLink,
                                          DateTime timestamp)
        {
            string alertTitle = _alertHelper.GetAlertTitle(alertDescription, alertVariables);
            
            var Payload = new Payload
            {
                Blocks = _slackAlertHelper.BuildSlackPayload(alertEmoji,
                                       timestamp,
                                       alertVariables["JobId"],
                                       alertVariables["AcademicYear"],
                                       alertVariables["CollectionPeriod"],
                                       alertVariables["CollectionPeriodPayments"],
                                       alertVariables["YearToDatePayments"],
                                       alertVariables["NumberOfLearners"],
                                       alertVariables["AccountedForPayments"],
                                       alertTitle,
                                       appInsightsSearchResultsUiLink)
            };
            
            var serializeOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
            var jsonData = JsonSerializer.Serialize(slackPayload, serializeOptions);

            _logger.LogInformation($"JSON payload sending to Slack API: {jsonData} ");

            await _slackClient.PostAsJsonAsync(slackChannelUri, jsonData);
        }
    }
}