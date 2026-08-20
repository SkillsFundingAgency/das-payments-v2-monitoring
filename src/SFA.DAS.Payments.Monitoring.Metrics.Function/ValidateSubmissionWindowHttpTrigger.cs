using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using SFA.DAS.Payments.Monitoring.Metrics.Application.Submission;

namespace SFA.DAS.Payments.Monitoring.Metrics.Function
{
    public static class ValidateSubmissionWindowHttpTrigger
    {
        [Function("ValidateSubmissionWindow")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            FunctionContext context)
        {
            var submissionWindowValidationService = context.InstanceServices.GetRequiredService<ISubmissionWindowValidationService>();

            long.TryParse(req.Query["jobId"], out var jobId);
            short.TryParse(req.Query["academicYear"], out var academicYear);
            byte.TryParse(req.Query["collectionPeriod"], out var collectionPeriod);

            var result = await submissionWindowValidationService.ValidateSubmissionWindow(jobId, academicYear, collectionPeriod, CancellationToken.None);

            if (result == null)
                throw new ApplicationException("Error in Submission Window Validation");

            if (result.IsWithinTolerance == false) //406
                return new BadRequestObjectResult("") {StatusCode = StatusCodes.Status406NotAcceptable};

            return new OkObjectResult(result); //200
        }
    }
}
