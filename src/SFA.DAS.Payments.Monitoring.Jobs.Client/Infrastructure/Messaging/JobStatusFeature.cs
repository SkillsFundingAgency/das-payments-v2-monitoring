using NServiceBus;
using NServiceBus.Features;
using NServiceBus.Pipeline;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

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

        //TODO: Figure out generic way to intercept failed messages
        //endpointConfig.Recoverability().Failed(
        //    failedMessage =>
        //    {
        //        failedMessage.OnMessageSentToErrorQueue((message, token) =>
        //        {
        //            var factory = c.Resolve<IJobMessageClientFactory>();
        //            var client = factory.Create();
        //            client.ProcessingFailedForJobMessage(message.Body.ToArray()).Wait(2000);
        //            return Task.CompletedTask;
        //        });
        //    }
        //);
    }
}

//public class EnableExternalBodyStorageBehavior : Behavior<IRecoverabilityContext>
//{

//    public EnableExternalBodyStorageBehavior()
//    {
//    }

//    public async override Task Invoke(IRecoverabilityContext context, Func<Task> next)
//    {
//        if (context.RecoverabilityAction is MoveToError errorAction)
//        {
//            var message = context.FailedMessage;
//            //var bodyUrl = await storage.StoreBody(message.MessageId, message.Body);

//            //context.Metadata["body-url"] = bodyUrl;

//            context.RecoverabilityAction = new SkipFailedMessageBody(errorAction.ErrorQueue);
//        }

//        await next();
//    }

//    class SkipFailedMessageBody : MoveToError
//    {
//        public SkipFailedMessageBody(string errorQueue) : base(errorQueue)
//        {
//        }

//        public override IReadOnlyCollection<IRoutingContext> GetRoutingContexts(IRecoverabilityActionContext context)
//        {
//            var routingContexts = base.GetRoutingContexts(context);

//            foreach (var routingContext in routingContexts)
//            {
//                // clear out the message body
//                routingContext.Message.UpdateBody(ReadOnlyMemory<byte>.Empty);
//            }

//            return routingContexts;
//        }
//    }
//}