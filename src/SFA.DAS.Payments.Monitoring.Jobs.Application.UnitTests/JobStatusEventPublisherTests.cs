﻿using System;
using System.Threading.Tasks;
using Autofac.Extras.Moq;
using Moq;
using NServiceBus;
using NUnit.Framework;
using SFA.DAS.Payments.Application.Messaging;
using SFA.DAS.Payments.MatchedLearner.Messages;
using SFA.DAS.Payments.Monitoring.Jobs.Application.Infrastructure;
using SFA.DAS.Payments.Monitoring.Jobs.Application.JobProcessing;
using SFA.DAS.Payments.Monitoring.Jobs.Messages.Events;

namespace SFA.DAS.Payments.Monitoring.Jobs.Application.UnitTests
{
    [TestFixture]
    public class JobStatusEventPublisherTests
    {
        private AutoMock mocker;
        private Mock<IEndpointInstance> matchedLearnerEndpointInstance;

        [SetUp]
        public void SetUp()
        {
            mocker = AutoMock.GetLoose();
            var endpointInstance = mocker.Mock<IEndpointInstance>();
            
            matchedLearnerEndpointInstance = mocker.Mock<IEndpointInstance>();
            mocker.Mock<IEndpointInstanceFactory>()
                .Setup(factory => factory.GetEndpointInstance())
                .ReturnsAsync(endpointInstance.Object);

            mocker.Mock<IMatchedLearnerEndpointFactory>()
                .Setup(factory => factory.GetEndpointInstanceAsync())
                .ReturnsAsync(matchedLearnerEndpointInstance.Object);
        }

        [Test]
        public async Task Publishes_SubmissionJobSucceeded_If_Job_Successful()
        {
            var submissionTime = DateTime.Now;
            await mocker.Create<JobStatusEventPublisher>()
                .SubmissionFinished(true, 99, 1234, 1920, 01, submissionTime);

            mocker.Mock<IEndpointInstance>()
                .Verify(x => x.Publish(It.Is<SubmissionJobSucceeded>(ev => 
                                                                          ev.JobId == 99 &&
                                                                          ev.AcademicYear == 1920 &&
                                                                          ev.CollectionPeriod == 01 &&
                                                                          ev.IlrSubmissionDateTime == submissionTime &&
                                                                          ev.Ukprn == 1234), It.IsAny<PublishOptions>()), Times.Once);
        }
    
        [Test]
        public async Task Publishes_SubmissionJobFailed_If_Job_Failed()
        {
            var submissionTime = DateTime.Now;
            await mocker.Create<JobStatusEventPublisher>()
                .SubmissionFinished(false, 99, 1234, 1920, 01, submissionTime);

            mocker.Mock<IEndpointInstance>()
                .Verify(x => x.Publish(It.Is<SubmissionJobFailed>(ev => 
                                                                               ev.JobId == 99 &&
                                                                               ev.AcademicYear == 1920 &&
                                                                               ev.CollectionPeriod == 01 &&
                                                                               ev.IlrSubmissionDateTime == submissionTime &&
                                                                               ev.Ukprn == 1234), It.IsAny<PublishOptions>()), Times.Once);
        }

        [Test]
        public async Task Publishes_SubmissionSucceededEvent_If_Job_Successful()
        {
            var submissionTime = DateTime.Now;
            await mocker.Create<JobStatusEventPublisher>()
                .SubmissionFinished(true, 99, 1234, 1920, 01, submissionTime);

            matchedLearnerEndpointInstance
                .Verify(x => x.Publish(It.Is<SubmissionSucceededEvent>(ev => 
                                                                          ev.JobId == 99 &&
                                                                          ev.AcademicYear == 1920 &&
                                                                          ev.CollectionPeriod == 01 &&
                                                                          ev.IlrSubmissionDateTime == submissionTime &&
                                                                          ev.Ukprn == 1234), It.IsAny<PublishOptions>()), Times.Once);
        }
    
        [Test]
        public async Task Publishes_SubmissionFailedEvent_If_Job_Failed()
        {
            var submissionTime = DateTime.Now;
            await mocker.Create<JobStatusEventPublisher>()
                .SubmissionFinished(false, 99, 1234, 1920, 01, submissionTime);

            matchedLearnerEndpointInstance
                .Verify(x => x.Publish(It.Is<SubmissionFailedEvent>(ev => 
                                                                               ev.JobId == 99 &&
                                                                               ev.AcademicYear == 1920 &&
                                                                               ev.CollectionPeriod == 01 &&
                                                                               ev.IlrSubmissionDateTime == submissionTime &&
                                                                               ev.Ukprn == 1234), It.IsAny<PublishOptions>()), Times.Once);
        }
    }
}