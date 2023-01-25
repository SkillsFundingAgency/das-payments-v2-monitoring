﻿using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;
using SFA.DAS.Payments.Monitoring.Alerts.Function.TypedClients;
using System;
using System.Net.Http;
using System.Net.Http.Headers;

[assembly: FunctionsStartup(typeof(SFA.DAS.Monitoring.Alerts.Function.Startup))]
namespace SFA.DAS.Monitoring.Alerts.Function
{
    public class Startup : FunctionsStartup
    {
        private static readonly int _numberOfRetries = 4;

        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddLogging();

            builder.Services
                .AddHttpClient<IAppInsightsClient, AppInsightsClient>(x =>
                {
                    var appInsightsAPIKeyHeader = GetEnvironmentVariable("AppInsightsAuthHeader");
                    var appInsightsAPIKeyValue = GetEnvironmentVariable("AppInsightsAuthValue");

                    x.DefaultRequestHeaders.Accept.Clear();
                    x.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    x.DefaultRequestHeaders.Add(appInsightsAPIKeyHeader, appInsightsAPIKeyValue);
                })
                .AddPolicyHandler(GetDefaultRetryPolicy());

            builder.Services
                .AddHttpClient<ISlackClient, SlackClient>(x =>
                {
                    x.BaseAddress = new Uri(GetEnvironmentVariable("SlackBaseUrl"));
                });
        }

        static IAsyncPolicy<HttpResponseMessage> GetDefaultRetryPolicy()
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.NotFound)
                .WaitAndRetryAsync(
                    _numberOfRetries, 
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), 
                    onRetry: (outcome, timespan, retryAttempt, context) =>
                    {
                        var log = context.GetLogger();
                        log?.LogInformation($"Request failed with status code {outcome.Result.StatusCode} delaying for {timespan.TotalMilliseconds} milliseconds then retry {retryAttempt}");
                    });
        }

        private static string GetEnvironmentVariable(string variableName)
        {
            return Environment.GetEnvironmentVariable(variableName, EnvironmentVariableTarget.Process);
        }
    }
}