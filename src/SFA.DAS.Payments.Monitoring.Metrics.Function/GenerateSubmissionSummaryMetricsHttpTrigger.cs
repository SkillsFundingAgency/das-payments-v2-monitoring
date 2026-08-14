using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using SFA.DAS.Payments.Monitoring.Metrics.Application.Submission;

namespace SFA.DAS.Payments.Monitoring.Metrics.Function
{
    public static class GenerateSubmissionSummaryMetricsHttpTrigger
    {
        [Function("SubmissionRequestReports")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            FunctionContext context)
        {
            var submissionMetricsService = context.InstanceServices.GetRequiredService<ISubmissionMetricsService>();

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
