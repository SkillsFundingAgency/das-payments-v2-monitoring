using System;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Pipeline;

namespace SFA.DAS.Payments.Monitoring.Jobs.Client.Infrastructure.Messaging;

public class JobStatusFailedMessageBehaviour : Behavior<IRecoverabilityContext>
{
    private readonly IJobMessageClientFactory factory;

    public JobStatusFailedMessageBehaviour(IJobMessageClientFactory factory)
    {
        this.factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    public override async Task Invoke(IRecoverabilityContext context, Func<Task> next)
    {
        if (context.RecoverabilityAction is MoveToError action)
        {
            var client = factory.Create();
            await client.ProcessingFailedForJobMessage(context.FailedMessage.Body.ToArray());
        }
        await next();
    }
}