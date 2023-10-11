using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SFA.DAS.Payments.Monitoring.Alerts.Function.Services
{
    public interface ISlackService
    {
        Task PostSlackAlert(string appInsightsAlertPayload, string slackChannelUri);

        Task PostSlackAlert(Dictionary<string, string> alertVariables, string slackChannelUri, string alertDescription, string alertEmoji, DateTime timestamp, string alertTitle);
    }
}