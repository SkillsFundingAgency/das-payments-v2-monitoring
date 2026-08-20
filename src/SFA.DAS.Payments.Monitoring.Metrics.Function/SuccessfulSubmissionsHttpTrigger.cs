using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using SFA.DAS.Payments.Monitoring.Metrics.Application.Submission;

namespace SFA.DAS.Payments.Monitoring.Metrics.Function
{
    public static class SuccessfulSubmissionsHttpTrigger
    {
        [Function("SuccessfulSubmission")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "v1/Submission/Successful")] HttpRequest req,
            FunctionContext context)
        {
            var submissionService = context.InstanceServices.GetRequiredService<ISubmissionJobsService>();

            short.TryParse(req.Query["academicYear"], out var academicYear);
            byte.TryParse(req.Query["collectionPeriod"], out var collectionPeriod);

            var results = await submissionService.SuccessfulSubmissionsForCollectionPeriod(academicYear, collectionPeriod);
            return new OkObjectResult(results);
        }
    }
}
