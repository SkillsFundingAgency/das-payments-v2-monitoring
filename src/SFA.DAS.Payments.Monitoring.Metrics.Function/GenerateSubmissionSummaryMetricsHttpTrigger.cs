using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AzureFunctions.Autofac;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using SFA.DAS.Payments.Monitoring.Metrics.Application.Submission;
using SFA.DAS.Payments.Monitoring.Metrics.Function.Infrastructure.IoC;

namespace SFA.DAS.Payments.Monitoring.Metrics.Function
{
    [DependencyInjectionConfig(typeof(DependencyRegister))]
    public static class GenerateSubmissionSummaryMetricsHttpTrigger
    {
        [FunctionName("SubmissionRequestReports")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            [Inject] ISubmissionMetricsService submissionMetricsService)
        {
            var validAcademicYear = short.TryParse(req.Query["academicYear"], out var academicYear);
            var validCollectionPeriod = byte.TryParse(req.Query["collectionPeriod"], out var collectionPeriod);
            if (!validAcademicYear || !validCollectionPeriod)
            {
                return new BadRequestResult();
            }

            var ukprns = ParseUkprnQueryString(req);
            if (ukprns == null)
            {
                return new BadRequestResult();
            }

            var ukprnJobIds = new Dictionary<long, long>();
            foreach (var ukprn in ukprns)
            {
                try
                {
                    var jobId = await submissionMetricsService.GetLatestSuccessfulJobIdForProvider(ukprn, academicYear, collectionPeriod);
                    ukprnJobIds.Add(ukprn, jobId);
                }
                catch (ArgumentException) // Latest Successful JobId not found
                {
                    return new BadRequestResult();
                }
            }

            foreach (var item in ukprnJobIds)
            {
                await submissionMetricsService.BuildMetrics(ukprn: item.Key, jobId: item.Value, academicYear, collectionPeriod, CancellationToken.None);
            }

            return new OkResult();
        }

        private static List<long> ParseUkprnQueryString(HttpRequest req)
        {
            try
            {
                var ukprns = req.Query["ukprns"].ToString().Split(',');

                var ukprnList = new List<long>();
                foreach (var ukprnString in ukprns)
                {
                    ukprnList.Add(Convert.ToInt64(ukprnString));
                }

                return ukprnList;
            }
            catch (FormatException)
            {
                return null;
            }
        }
    }
}