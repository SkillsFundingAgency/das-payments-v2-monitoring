﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NServiceBus.Pipeline;
using SFA.DAS.Payments.Messages.Core;
using SFA.DAS.Payments.Monitoring.Jobs.Messages.Commands;

namespace SFA.DAS.Payments.Monitoring.Jobs.Client.Infrastructure.Messaging
{
    public class JobStatusIncomingMessageBehaviour : Behavior<IIncomingLogicalMessageContext>
    {
        private readonly IProviderEarningsJobStatusClientFactory factory;

        public JobStatusIncomingMessageBehaviour(IProviderEarningsJobStatusClientFactory factory)
        {
            this.factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        public override async Task Invoke(IIncomingLogicalMessageContext context, Func<Task> next)
        {
            var eventIds = new List<GeneratedMessage>();
            var paymentMessage = context.Message.Instance as IPaymentsMessage;
            if (paymentMessage != null)
            {
                context.Extensions.Set("event_ids", eventIds);
            }

            await next().ConfigureAwait(false);

            if (paymentMessage == null)
                return;
            var jobStatusClient = factory.Create();
            await jobStatusClient.ProcessedJobMessage(paymentMessage.JobId, context.GetMessageId(), eventIds)
                .ConfigureAwait(false);
        }
    }
}