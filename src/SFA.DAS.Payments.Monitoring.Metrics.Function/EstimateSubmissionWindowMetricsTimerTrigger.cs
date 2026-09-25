using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using SFA.DAS.Payments.Monitoring.Metrics.Application.Submission;

namespace SFA.DAS.Payments.Monitoring.Metrics.Function
{
    public static class EstimateSubmissionWindowMetricsTimerTrigger
    {
        [Function("EstimateSubmissionWindowMetrics")]
        public static async Task RunOnTimer(
            [TimerTrigger("%EstimateSubmissionWindowMetricsSchedule%", RunOnStartup=false)]TimerInfo myTimer,
            FunctionContext context)
        {
            var submissionWindowValidationService = context.InstanceServices.GetRequiredService<ISubmissionWindowValidationService>();

            await submissionWindowValidationService.EstimateSubmissionWindowMetrics();
        }
    }
}
