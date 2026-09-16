using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus;

namespace SFA.DAS.Payments.Monitoring.Tests.Specs.StepDefinitions
{
    public class RecordingMessageSession : IMessageSession
    {
        public ConcurrentBag<object> SentMessages { get; } = new ConcurrentBag<object>();

        public Task Send(object message, SendOptions options, CancellationToken cancellationToken = default)
        {
            SentMessages.Add(message);
            return Task.CompletedTask;
        }

        public Task Send<T>(Action<T> messageConstructor, SendOptions options, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task Publish(object message, PublishOptions options, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task Publish<T>(Action<T> messageConstructor, PublishOptions options, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task Subscribe(Type eventType, SubscribeOptions options, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task Unsubscribe(Type eventType, UnsubscribeOptions options, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
