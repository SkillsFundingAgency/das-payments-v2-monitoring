using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using NServiceBus.Extensibility;
using NServiceBus.Pipeline;
using NServiceBus.Testing;
using NServiceBus.Unicast.Messages;
using NUnit.Framework;
using SFA.DAS.Payments.Messages.Common;
using SFA.DAS.Payments.Messages.Common.Events;
using SFA.DAS.Payments.Monitoring.Jobs.Client.Infrastructure.Messaging;
using SFA.DAS.Payments.Monitoring.Jobs.Client.UnitTests.Models;
using SFA.DAS.Payments.Monitoring.Jobs.Messages.Commands;

namespace SFA.DAS.Payments.Monitoring.Jobs.Client.UnitTests
{
    [TestFixture]
    public class JobStatusIncomingMessageBehaviourTests
    {
        private Mock<IJobMessageClientFactory> factoryMock;
        private Mock<IJobMessageClient> jobMessageClientMock;
        private TestableIncomingLogicalMessageContext context;
        private JobStatusIncomingMessageBehaviour sut;

        [SetUp]
        public void SetUp()
        {
            jobMessageClientMock = new Mock<IJobMessageClient>();

            factoryMock = new Mock<IJobMessageClientFactory>();
            factoryMock.Setup(x => x.Create()).Returns(jobMessageClientMock.Object);

            context = new TestableIncomingLogicalMessageContext
            {
                Extensions = new ContextBag()
            };

            sut = new JobStatusIncomingMessageBehaviour(factoryMock.Object);
        }

        [Test]
        public async Task Invoke_MonitoredMessageWithNonZeroJobId_ProcessesJobMessage()
        {
            var message = new TestMonitoredMessage { JobId = 123, EventId = Guid.NewGuid() };
            context.Message = new LogicalMessage(new MessageMetadata(typeof(TestMonitoredMessage)), message);

            await sut.Invoke(context, () => Task.CompletedTask);

            jobMessageClientMock.Verify(x => x.ProcessedJobMessage(
                123,
                message.EventId,
                nameof(TestMonitoredMessage),
                It.IsAny<List<GeneratedMessage>>()), Times.Once);
        }

        [Test]
        public async Task Invoke_MonitoredMessageWithZeroJobId_DoesNotProcessJobMessage()
        {
            var message = new TestMonitoredMessage { JobId = 0, EventId = Guid.NewGuid() };
            context.Message = new LogicalMessage(new MessageMetadata(typeof(TestMonitoredMessage)), message);

            await sut.Invoke(context, () => Task.CompletedTask);

            factoryMock.Verify(x => x.Create(), Times.Never);
        }

        [Test]
        public async Task Invoke_MessageIsNotMonitoredMessage_DoesNotProcessJobMessage()
        {
            var message = new UnmonitoredMessage();
            context.Message = new LogicalMessage(new MessageMetadata(typeof(UnmonitoredMessage)), message);

            await sut.Invoke(context, () => Task.CompletedTask);

            factoryMock.Verify(x => x.Create(), Times.Never);
        }

        [Test]
        public async Task Invoke_SetsGeneratedMessagesOnContextExtensionsBeforeCallingNext()
        {
            var message = new TestMonitoredMessage { JobId = 1, EventId = Guid.NewGuid() };
            context.Message = new LogicalMessage(new MessageMetadata(typeof(TestMonitoredMessage)), message);

            List<GeneratedMessage> capturedMessages = null;
            Func<Task> next = () =>
            {
                capturedMessages = context.Extensions.Get<List<GeneratedMessage>>(JobStatusBehaviourConstants.GeneratedMessagesKey);
                return Task.CompletedTask;
            };

            await sut.Invoke(context, next);

            Assert.That(capturedMessages, Is.Not.Null);
        }
    }
}
