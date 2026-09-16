using System;
using System.Threading.Tasks;
using NServiceBus;
using SFA.DAS.Payments.Monitoring.Tests.Specs.Messages;

namespace SFA.DAS.Payments.Monitoring.Tests.Specs.Handlers
{
    public class TestMonitoredCommandHandler : IHandleMessages<TestMonitoredCommand>
    {
        public async Task Handle(TestMonitoredCommand message, IMessageHandlerContext context)
        {
            if (message.ShouldFail)
            {
                throw new InvalidOperationException("Simulated processing failure to trigger the Job Status Failed Message behaviour.");
            }

            await context.Publish(new TestMonitoredEvent { JobId = message.JobId });
        }
    }
}
