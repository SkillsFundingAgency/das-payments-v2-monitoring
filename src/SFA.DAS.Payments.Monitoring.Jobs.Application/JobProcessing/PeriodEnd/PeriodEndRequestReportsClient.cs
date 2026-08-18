using Newtonsoft.Json;
using SFA.DAS.Payments.Application.Infrastructure.Logging;
using SFA.DAS.Payments.Monitoring.Metrics.Model.PeriodEnd;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace SFA.DAS.Payments.Monitoring.Jobs.Application.JobProcessing.PeriodEnd
{
    public interface IPeriodEndRequestReportsClient
    {
        Task<bool> RequestReports(long jobId, short academicYear, byte collectionPeriod);
    }

    public class PeriodEndRequestReportsClient : IPeriodEndRequestReportsClient
    {
        private readonly string authCode;
        private readonly Uri functionAddressUri;
        private readonly IPaymentLogger logger;

        public PeriodEndRequestReportsClient(string authCode, string functionAddress, IPaymentLogger logger)
        {
            this.authCode = authCode;
            functionAddressUri = new Uri(functionAddress);
            this.logger = logger;
        }

        public async Task<bool> RequestReports(long jobId, short academicYear, byte collectionPeriod)
        {
            var result = await new HttpClient { Timeout = TimeSpan.FromSeconds(270) }.GetAsync(BuildUriFromParameters(jobId, academicYear, collectionPeriod));

            if (!result.IsSuccessStatusCode)
            {
                var response = await result.Content.ReadAsStringAsync();
                logger.LogError($"Error retrieving period end reports, HTTP Status Code {result.StatusCode}, Response Message: {response}");
                return false;
            }

            var content = await result.Content.ReadAsStringAsync();
            var periodEndSummaryModel = JsonConvert.DeserializeObject<PeriodEndSummaryModel>(content);
            return periodEndSummaryModel.IsWithinTolerance;
        }

        private string BuildUriFromParameters(long jobId, short academicYear, byte collectionPeriod)
        {
            return string.IsNullOrWhiteSpace(authCode)
                ? $"{new Uri(functionAddressUri, "/api/PeriodEndRequestReports")}?jobId={jobId}&collectionPeriod={collectionPeriod}&AcademicYear={academicYear}"
                : $"{new Uri(functionAddressUri, "/api/PeriodEndRequestReports")}?code={authCode}&jobId={jobId}&collectionPeriod={collectionPeriod}&AcademicYear={academicYear}";
        }
    }
}