using NServiceBus.Features;

namespace SFA.DAS.Payments.Monitoring.Jobs.Client.Infrastructure.Messaging;

public class JobStatusFeature : Feature
{
    public JobStatusFeature()
    {
        EnableByDefault();
    }

    protected override void Setup(FeatureConfigurationContext context)
    {
        context.Pipeline.Register(typeof(JobStatusIncomingMessageBehaviour),
            "Job Status Incoming message behaviour");
        context.Pipeline.Register(typeof(JobStatusOutgoingMessageBehaviour),
            "Job Status Outgoing message behaviour");
        context.Pipeline.Register(typeof(JobStatusFailedMessageBehaviour), 
            "Job Status Failed Message Behaviour");
    }
}