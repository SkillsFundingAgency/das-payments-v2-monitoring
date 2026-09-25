using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using SFA.DAS.Payments.Monitoring.Metrics.Application.PeriodEnd;

namespace SFA.DAS.Payments.Monitoring.Metrics.Function
{
    public static class PeriodEndRequestReportsHttpTrigger
    {
        [Function("PeriodEndRequestReports")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            FunctionContext context)
        {
            var periodEndMetricsService = context.InstanceServices.GetRequiredService<IPeriodEndMetricsService>();

            long.TryParse(req.Query["jobId"], out var jobId);
            short.TryParse(req.Query["academicYear"], out var academicYear);
            byte.TryParse(req.Query["collectionPeriod"], out var collectionPeriod);

            var result = await periodEndMetricsService.BuildMetrics(jobId, academicYear, collectionPeriod, CancellationToken.None);

            if (result == null)
                throw new ApplicationException("Error in Period End Request Reports");

            return new OkObjectResult(result); //200
        }
    }
}
