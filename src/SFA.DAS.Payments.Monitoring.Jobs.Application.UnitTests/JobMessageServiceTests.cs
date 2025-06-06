﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autofac.Extras.Moq;
using Moq;
using NUnit.Framework;
using SFA.DAS.Payments.Monitoring.Jobs.Application.JobMessageProcessing;
using SFA.DAS.Payments.Monitoring.Jobs.DataMessages.Commands;
using SFA.DAS.Payments.Monitoring.Jobs.Model;

namespace SFA.DAS.Payments.Monitoring.Jobs.Application.UnitTests
{
    [TestFixture]
    public class JobMessageServiceTests
    {
        private AutoMock mocker;

        [SetUp]
        public void SetUp()
        {
            mocker = AutoMock.GetLoose();

            mocker.Mock<IJobStorageService>()
                .Setup(x => x.GetInProgressMessages(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<InProgressMessage>());
        }

        [Test]
        public async Task Stores_Completed_Job_Message()
        {
            var jobStatusMessage = new RecordJobMessageProcessingStatus
            {
                Id = Guid.NewGuid(),
                JobId = 1,
                GeneratedMessages = new List<GeneratedMessage>(),
                EndTime = DateTime.UtcNow,
                Succeeded = true
            };
            var service = mocker.Create<JobMessageService>();
            await service.RecordCompletedJobMessageStatus(jobStatusMessage, default(CancellationToken));
            mocker.Mock<IJobStorageService>()
                .Verify(
                    x => x.StoreCompletedMessage(It.Is<CompletedMessage>(msg =>
                        msg.MessageId == jobStatusMessage.Id && msg.CompletedTime == jobStatusMessage.EndTime), It.IsAny<CancellationToken>()),
                    Times.Once);
        }

        [Test]
        public async Task Stores_In_Progress_Messages()
        {
            var generatedMessageA = new GeneratedMessage
            {

                StartTime = DateTimeOffset.UtcNow,
                MessageId = Guid.NewGuid(),
                MessageName = "MessageA",
            };
            var generatedMessageB = new GeneratedMessage
            {

                StartTime = DateTimeOffset.UtcNow,
                MessageId = Guid.NewGuid(),
                MessageName = "MessageB",
            };
            var jobStatusMessage = new RecordJobMessageProcessingStatus
            {
                Id = Guid.NewGuid(),
                JobId = 1,
                GeneratedMessages = new List<GeneratedMessage> { generatedMessageA, generatedMessageB },
                EndTime = DateTime.UtcNow,
                Succeeded = true
            };

            var service = mocker.Create<JobMessageService>();
            await service.RecordCompletedJobMessageStatus(jobStatusMessage, CancellationToken.None);

            mocker.Mock<IJobStorageService>()
                .Verify(x => x.StoreInProgressMessages(It.Is<long>(jobId => jobId == jobStatusMessage.JobId), It.Is<List<InProgressMessage>>(identifiers =>
                    identifiers.Count == 2 &&
                    identifiers.Exists(inProgress => inProgress.MessageId == generatedMessageA.MessageId) &&
                    identifiers.Exists(inProgress => inProgress.MessageId == generatedMessageB.MessageId)), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}