using SFA.DAS.Payments.Monitoring.Alerts.Function.Models;
using System;
using System.Collections.Generic;

namespace SFA.DAS.Payments.Monitoring.Alerts.Function.Helpers
{
    public interface IAlertHelper
    {
        public string GetEmoji(string severity);

        public List<Block> BuildPayload(string alertEmoji,
                                              DateTime timestamp,
                                              string jobId,
                                              string academicYear,
                                              string collectionPeriod,
                                              string collectionPeriodPayments,
                                              string yearToDatePayments,
                                              string numberOfLearners,
                                              string accountedForPayments,
                                              string alertTitle,
                                              string appInsightsSearchResultsUiLink);

        public Dictionary<string, string> ExtractAlertVariables(dynamic customMeasurements, dynamic customDimensions, DateTime timestamp);

        public string GetAlertTitle(string alertTitleFormat, Dictionary<string, string> alertVariables);

        public string GetAlertText(string alertTextFormat, Dictionary<string, string> alertVariables);
    }
}
