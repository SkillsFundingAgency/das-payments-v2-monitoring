using NServiceBus.Extensibility;
using NServiceBus.Pipeline;
using NServiceBus.Testing;
using NUnit.Framework;
using SFA.DAS.Payments.Monitoring.Jobs.Client.Infrastructure.Messaging;
using SFA.DAS.Payments.Monitoring.Jobs.Client.UnitTests.Models;
using SFA.DAS.Payments.Monitoring.Jobs.Messages.Commands;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SFA.DAS.Payments.Monitoring.Jobs.Client.UnitTests
{
    [TestFixture]
    public class JobStatusOutgoingMessageBehaviourTests
    {
        private TestableOutgoingLogicalMessageContext context;
        private JobStatusOutgoingMessageBehaviour sut;

        [SetUp]
        public void SetUp()
        {
            context = new TestableOutgoingLogicalMessageContext
            {
                Extensions = new ContextBag()
            };

            sut = new JobStatusOutgoingMessageBehaviour();
        }

        [Test]
        public async Task Invoke_MonitoredMessageWithNonZeroJobIdAndGeneratedMessagesListPresent_AddsGeneratedMessage()
        {
            var generatedMessages = new List<GeneratedMessage>();
            context.Extensions.Set(JobStatusBehaviourConstants.GeneratedMessagesKey, generatedMessages);

            var message = new TestMonitoredMessage { JobId = 5, EventId = Guid.NewGuid() };
            context.Message = new OutgoingLogicalMessage(typeof(TestMonitoredMessage), message);

            await sut.Invoke(context, () => Task.CompletedTask);

            Assert.That(generatedMessages, Has.Count.EqualTo(1));
            Assert.That(generatedMessages[0].MessageId, Is.EqualTo(message.EventId));
            Assert.That(generatedMessages[0].MessageName, Is.EqualTo(nameof(TestMonitoredMessage)));
        }

        [Test]
        public async Task Invoke_MonitoredMessageWithZeroJobId_DoesNotAddGeneratedMessage()
        {
            var generatedMessages = new List<GeneratedMessage>();
            context.Extensions.Set(JobStatusBehaviourConstants.GeneratedMessagesKey, generatedMessages);

            var message = new TestMonitoredMessage { JobId = 0, EventId = Guid.NewGuid() };
            context.Message = new OutgoingLogicalMessage(typeof(TestMonitoredMessage), message);

            await sut.Invoke(context, () => Task.CompletedTask);

            Assert.That(generatedMessages, Is.Empty);
        }

        [Test]
        public async Task Invoke_MessageIsNotMonitoredMessage_DoesNotAddGeneratedMessage()
        {
            var generatedMessages = new List<GeneratedMessage>();
            context.Extensions.Set(JobStatusBehaviourConstants.GeneratedMessagesKey, generatedMessages);

            var message = new UnmonitoredMessage();
            context.Message = new OutgoingLogicalMessage(typeof(UnmonitoredMessage), message);

            await sut.Invoke(context, () => Task.CompletedTask);

            Assert.That(generatedMessages, Is.Empty);
        }

        [Test]
        public void Invoke_GeneratedMessagesListIsNotInExtensions_DoesNotThrow()
        {
            var message = new TestMonitoredMessage { JobId = 5, EventId = Guid.NewGuid() };
            context.Message = new OutgoingLogicalMessage(typeof(TestMonitoredMessage), message);

            Assert.DoesNotThrowAsync(() => sut.Invoke(context, () => Task.CompletedTask));
        }
    }
}
