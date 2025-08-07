using System;
using System.Threading;
using System.Threading.Tasks;
using AzureFunctions.Autofac;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using SFA.DAS.Payments.Monitoring.Metrics.Application.PeriodEnd;
using SFA.DAS.Payments.Monitoring.Metrics.Application.Submission;
using SFA.DAS.Payments.Monitoring.Metrics.Function.Infrastructure.IoC;

namespace SFA.DAS.Payments.Monitoring.Metrics.Function
{
    [DependencyInjectionConfig(typeof(DependencyRegister))]
    public static class GenerateSubmissionSummaryMetricssHttpTrigger
    {
        [FunctionName("SubmissionRequestReports")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            [Inject] ISubmissionMetricsService submissionMetricsService)
        {
            long.TryParse(req.Query["jobId"], out var jobId);
            short.TryParse(req.Query["academicYear"], out var academicYear);
            byte.TryParse(req.Query["collectionPeriod"], out var collectionPeriod);
            long.TryParse(req.Query["ukprn"], out var ukprn);

            await submissionMetricsService.BuildMetrics(ukprn, jobId, academicYear, collectionPeriod, CancellationToken.None);
            
            return new OkResult(); 
        }
    }
}