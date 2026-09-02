using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NServiceBus;
using NUnit.Framework;
using SFA.DAS.Payments.Application.Infrastructure.Logging;
using SFA.DAS.Payments.Core.Configuration;
using SFA.DAS.Payments.Monitoring.Jobs.Messages.Commands;

namespace SFA.DAS.Payments.Monitoring.Jobs.Client.UnitTests
{
    [TestFixture]
    public class JobMessageClientTests
    {
        private Mock<IMessageSession> messageSessionMock;
        private Mock<IPaymentLogger> loggerMock;
        private Mock<IConfigurationHelper> configMock;

        private JobMessageClient sut;

        [SetUp]
        public void SetUp()
        {
            messageSessionMock = new Mock<IMessageSession>();
            loggerMock = new Mock<IPaymentLogger>();
            configMock = new Mock<IConfigurationHelper>();

            sut = new JobMessageClient(messageSessionMock.Object, loggerMock.Object, configMock.Object);
        }

        [Test]
        public async Task ProcessingFailedForJobMessage_WhenJobIdIsZero_DoesNotSendMessage()
        {
            //Arrange
            var body = BuildFailedMessageBody(Guid.NewGuid(), 0);

            //Act
            await sut.ProcessingFailedForJobMessage(body);

            //Assert
            messageSessionMock.Verify(x => x.Send(It.IsAny<object>(), It.IsAny<SendOptions>(), CancellationToken.None), Times.Never());
        }

        [Test]
        public async Task ProcessingFailedForJobMessage_WhenJobIdIsNonZero_SendsMessage()
        {
            //Arrange
            var body = BuildFailedMessageBody(Guid.NewGuid(), 123);

            //Act
            await sut.ProcessingFailedForJobMessage(body);

            //Assert
            messageSessionMock.Verify(x => x.Send(It.IsAny<object>(), It.IsAny<SendOptions>(), CancellationToken.None), Times.Once());
        }

        private static byte[] BuildFailedMessageBody(Guid eventId, long jobId)
        {
            var json = $"{{\"EventId\":\"{eventId}\",\"JobId\":\"{jobId}\"}}";
            return Encoding.UTF8.GetBytes(json);
        }
    }
}
