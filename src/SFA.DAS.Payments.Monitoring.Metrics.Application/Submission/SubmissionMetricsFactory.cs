using System;
using Autofac;

namespace SFA.DAS.Payments.Monitoring.Metrics.Application.Submission
{
    
    public interface ISubmissionMetricsFactory
    {
        ISubmissionMetricsRepository Create();
    }

    public class SubmissionMetricsFactory : ISubmissionMetricsFactory
    {
        private readonly ILifetimeScope scope;

        public SubmissionMetricsFactory(ILifetimeScope scope)
        {
            this.scope = scope ?? throw new ArgumentNullException(nameof(scope));
        }

        public ISubmissionMetricsRepository Create()
        {
            return scope.Resolve<ISubmissionMetricsRepository>();
        }
    }
}
