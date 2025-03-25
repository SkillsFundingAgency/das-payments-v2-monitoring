using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Autofac.Core;
using Autofac.Core.Registration;
using Azure.Messaging.ServiceBus;
using FluentAssertions;
using Microsoft.Azure.ServiceBus.Management;
using Moq;
using NUnit.Framework;
using SFA.DAS.Payments.Application.Infrastructure.Ioc;
using SFA.DAS.Payments.Application.Infrastructure.Logging;
using SFA.DAS.Payments.Application.Infrastructure.Telemetry;
using SFA.DAS.Payments.Messaging.Serialization;
using SFA.DAS.Payments.Monitoring.Jobs.Application.Infrastructure.Messaging;
using SFA.DAS.Payments.Monitoring.Jobs.Messages.Commands;

namespace SFA.DAS.Payments.Monitoring.Jobs.Application.UnitTests.Messaging
{
    [TestFixture]
    public class JobBatchCommunicationListenerTests
    {
        private Mock<IPaymentLogger> logger;
        private Mock<IContainerScopeFactory> containerScopeFactory;
        private Mock<ITelemetry> telemetry;
        private Mock<IMessageDeserializer> messageDeserializer;
        private Mock<IApplicationMessageModifier> applicationMessageModifier;
        private Mock<IManagementClientFactory> managementClientFactory;
        private Mock<IServiceBusClientFactory> serviceBusClientFactory;
        private Mock<TestManagementClient> managementClient;
        private JobBatchCommunicationListener listener;
        private const string EndpointName = "endpoint";

        [SetUp]
        public void Setup()
        {
            logger = new Mock<IPaymentLogger>();
            containerScopeFactory = new Mock<IContainerScopeFactory>();
            telemetry = new Mock<ITelemetry>();
            messageDeserializer = new Mock<IMessageDeserializer>();
            applicationMessageModifier = new Mock<IApplicationMessageModifier>();
            managementClientFactory = new Mock<IManagementClientFactory>();
            serviceBusClientFactory = new Mock<IServiceBusClientFactory>();
            managementClient = new Mock<TestManagementClient>();
        }


        [Test]
        public void Constructor_throws_argument_exception_when_endpoint_name_is_null()
        {
            Assert.Throws<ArgumentNullException>(() =>
                listener = new JobBatchCommunicationListener(null, null, logger.Object,
                    containerScopeFactory.Object, telemetry.Object, messageDeserializer.Object,
                    applicationMessageModifier.Object, managementClientFactory.Object, serviceBusClientFactory.Object));
        }

        [Test]
        public void Constructor_throws_argument_exception_when_logger_is_null()
        {
            Assert.Throws<ArgumentNullException>(() =>
            listener = new JobBatchCommunicationListener(EndpointName, null, null,
                containerScopeFactory.Object, telemetry.Object, messageDeserializer.Object,
                applicationMessageModifier.Object, managementClientFactory.Object, serviceBusClientFactory.Object));
        }

        [Test]
        public void Constructor_throws_argument_exception_when_container_scope_factory_is_null()
        {
            Assert.Throws<ArgumentNullException>(() =>
            listener = new JobBatchCommunicationListener(EndpointName, null, logger.Object,
                null, telemetry.Object, messageDeserializer.Object,
                applicationMessageModifier.Object, managementClientFactory.Object, serviceBusClientFactory.Object));
        }

        [Test]
        public void Constructor_throws_argument_exception_when_telemetry_is_null()
        {
            Assert.Throws<ArgumentNullException>(() =>
                listener = new JobBatchCommunicationListener(EndpointName, null, logger.Object,
                    containerScopeFactory.Object, null, messageDeserializer.Object,
                    applicationMessageModifier.Object, managementClientFactory.Object, serviceBusClientFactory.Object));
        }

        [Test]
        public void Constructor_throws_argument_exception_when_message_deserializer_is_null()
        {
            Assert.Throws<ArgumentNullException>(() =>
                listener = new JobBatchCommunicationListener(EndpointName, null, logger.Object,
                    containerScopeFactory.Object, telemetry.Object, null,
                    applicationMessageModifier.Object, managementClientFactory.Object, serviceBusClientFactory.Object));
        }

        [Test]
        public void Constructor_throws_argument_exception_when_message_modifier_is_null()
        {
            Assert.Throws<ArgumentNullException>(() =>
                listener = new JobBatchCommunicationListener(EndpointName, null, logger.Object,
                    containerScopeFactory.Object, telemetry.Object, messageDeserializer.Object,
                    null, managementClientFactory.Object, serviceBusClientFactory.Object));
        }

        [Test]
        public void Constructor_throws_argument_exception_when_management_client_factory_is_null()
        {
            Assert.Throws<ArgumentNullException>(() =>
                listener = new JobBatchCommunicationListener(EndpointName, null, logger.Object,
                    containerScopeFactory.Object, telemetry.Object, messageDeserializer.Object,
                    applicationMessageModifier.Object, null, serviceBusClientFactory.Object));
        }

        [Test]
        public void Constructor_throws_argument_exception_when_service_bus_client_factory_is_null()
        {
            Assert.Throws<ArgumentNullException>(() =>
                listener = new JobBatchCommunicationListener(EndpointName, null, logger.Object,
                    containerScopeFactory.Object, telemetry.Object, messageDeserializer.Object,
                    applicationMessageModifier.Object, managementClientFactory.Object, null));
        }

        [Test]
        public async Task When_OpenAsync_is_called_queue_is_created_on_opening_as_does_not_exist()
        {
            // Arrange
            managementClient.Setup(x => x.QueueExistsAsync(EndpointName, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            managementClient.Setup(x => x.QueueExistsAsync($@"{EndpointName}-Errors", It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            var queueDescription = new QueueDescription(EndpointName)
            {
                DefaultMessageTimeToLive = TimeSpan.FromDays(7),
                EnableDeadLetteringOnMessageExpiration = true,
                LockDuration = TimeSpan.FromMinutes(5),
                MaxDeliveryCount = 50,
                MaxSizeInMB = 5120,
                Path = EndpointName
            };
            managementClient.Setup(x => x.CreateQueueAsync(queueDescription, It.IsAny<CancellationToken>())).ReturnsAsync(queueDescription);
            var errorQueueDescription = new QueueDescription($@"{EndpointName}-Errors")
            {
                DefaultMessageTimeToLive = TimeSpan.FromDays(7),
                EnableDeadLetteringOnMessageExpiration = true,
                LockDuration = TimeSpan.FromMinutes(5),
                MaxDeliveryCount = 50,
                MaxSizeInMB = 5120,
                Path = $@"{EndpointName}-Errors"
            };
            managementClient.Setup(x => x.CreateQueueAsync(errorQueueDescription, It.IsAny<CancellationToken>())).ReturnsAsync(errorQueueDescription);

            managementClientFactory.Setup(x => x.GetManagementClient()).Returns(managementClient.Object);

            // Act
            listener = new JobBatchCommunicationListener(EndpointName, null, logger.Object,
                containerScopeFactory.Object, telemetry.Object, messageDeserializer.Object,
                applicationMessageModifier.Object, managementClientFactory.Object, serviceBusClientFactory.Object);
            var result = await listener.OpenAsync(CancellationToken.None);

            // Assert
            result.Should().Be(EndpointName);
            managementClient.Verify(x => x.QueueExistsAsync(EndpointName, It.IsAny<CancellationToken>()), Times.Once);
            managementClient.Verify(x => x.QueueExistsAsync($@"{EndpointName}-Errors", It.IsAny<CancellationToken>()), Times.Once);
            managementClient.Verify(x => x.CreateQueueAsync(queueDescription, It.IsAny<CancellationToken>()), Times.Once);
            managementClient.Verify(x => x.CreateQueueAsync(errorQueueDescription, It.IsAny<CancellationToken>()), Times.Once);
        }
        
        [Test]
        public void When_OpenAsync_is_called_queue_is_not_created_on_opening_as_already_exists()
        {
            // Arrange
            managementClient.Setup(x => x.QueueExistsAsync(EndpointName, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            managementClient.Setup(x => x.QueueExistsAsync($@"{EndpointName}-Errors", It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            var queueDescription = new QueueDescription(EndpointName)
            {
                DefaultMessageTimeToLive = TimeSpan.FromDays(7),
                EnableDeadLetteringOnMessageExpiration = true,
                LockDuration = TimeSpan.FromMinutes(5),
                MaxDeliveryCount = 50,
                MaxSizeInMB = 5120,
                Path = EndpointName
            };
            managementClient.Setup(x => x.CreateQueueAsync(queueDescription, It.IsAny<CancellationToken>())).ReturnsAsync(queueDescription);
            var errorQueueDescription = new QueueDescription($@"{EndpointName}-Errors")
            {
                DefaultMessageTimeToLive = TimeSpan.FromDays(7),
                EnableDeadLetteringOnMessageExpiration = true,
                LockDuration = TimeSpan.FromMinutes(5),
                MaxDeliveryCount = 50,
                MaxSizeInMB = 5120,
                Path = $@"{EndpointName}-Errors"
            };
            managementClient.Setup(x => x.CreateQueueAsync(errorQueueDescription, It.IsAny<CancellationToken>())).ReturnsAsync(errorQueueDescription);

            managementClientFactory.Setup(x => x.GetManagementClient()).Returns(managementClient.Object);

            // Act

            listener = new JobBatchCommunicationListener(EndpointName, null, logger.Object,
                containerScopeFactory.Object, telemetry.Object, messageDeserializer.Object,
                applicationMessageModifier.Object, managementClientFactory.Object, serviceBusClientFactory.Object);
            listener.OpenAsync(CancellationToken.None);

            // Assert
            managementClient.Verify(x => x.QueueExistsAsync(EndpointName, It.IsAny<CancellationToken>()), Times.Once);
            managementClient.Verify(x => x.QueueExistsAsync($@"{EndpointName}-Errors", It.IsAny<CancellationToken>()), Times.Once);
            managementClient.Verify(x => x.CreateQueueAsync(queueDescription, It.IsAny<CancellationToken>()), Times.Never);
            managementClient.Verify(x => x.CreateQueueAsync(errorQueueDescription, It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public void When_OpenAsync_is_called_default_error_queue_is_used_if_not_specifically_defined()
        {
            // Arrange
            managementClient.Setup(x => x.QueueExistsAsync(EndpointName, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            managementClient.Setup(x => x.QueueExistsAsync($@"{EndpointName}-Errors", It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            var queueDescription = new QueueDescription(EndpointName)
            {
                DefaultMessageTimeToLive = TimeSpan.FromDays(7),
                EnableDeadLetteringOnMessageExpiration = true,
                LockDuration = TimeSpan.FromMinutes(5),
                MaxDeliveryCount = 50,
                MaxSizeInMB = 5120,
                Path = EndpointName
            };
            managementClient.Setup(x => x.CreateQueueAsync(queueDescription, It.IsAny<CancellationToken>())).ReturnsAsync(queueDescription);
            var errorQueueDescription = new QueueDescription($@"{EndpointName}-Errors")
            {
                DefaultMessageTimeToLive = TimeSpan.FromDays(7),
                EnableDeadLetteringOnMessageExpiration = true,
                LockDuration = TimeSpan.FromMinutes(5),
                MaxDeliveryCount = 50,
                MaxSizeInMB = 5120,
                Path = $@"{EndpointName}-Errors"
            };
            managementClient.Setup(x => x.CreateQueueAsync(errorQueueDescription, It.IsAny<CancellationToken>())).ReturnsAsync(errorQueueDescription);

            managementClientFactory.Setup(x => x.GetManagementClient()).Returns(managementClient.Object);

            // Act

            listener = new JobBatchCommunicationListener(EndpointName, null, logger.Object,
                containerScopeFactory.Object, telemetry.Object, messageDeserializer.Object,
                applicationMessageModifier.Object, managementClientFactory.Object, serviceBusClientFactory.Object);
            listener.OpenAsync(CancellationToken.None);

            // Assert
            managementClient.Verify(x => x.QueueExistsAsync($@"{EndpointName}-Errors", It.IsAny<CancellationToken>()), Times.Once());
        }

        [Test]
        public void When_OpenAsync_is_called_configured_error_queue_is_used_if_defined()
        {
            // Arrange
            var errorQueueName = "CombinedErrorsQueue";
            managementClient.Setup(x => x.QueueExistsAsync(EndpointName, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            managementClient.Setup(x => x.QueueExistsAsync(errorQueueName, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            var queueDescription = new QueueDescription(EndpointName)
            {
                DefaultMessageTimeToLive = TimeSpan.FromDays(7),
                EnableDeadLetteringOnMessageExpiration = true,
                LockDuration = TimeSpan.FromMinutes(5),
                MaxDeliveryCount = 50,
                MaxSizeInMB = 5120,
                Path = EndpointName
            };
            managementClient.Setup(x => x.CreateQueueAsync(queueDescription, It.IsAny<CancellationToken>())).ReturnsAsync(queueDescription);
            var errorQueueDescription = new QueueDescription($@"{EndpointName}-Errors")
            {
                DefaultMessageTimeToLive = TimeSpan.FromDays(7),
                EnableDeadLetteringOnMessageExpiration = true,
                LockDuration = TimeSpan.FromMinutes(5),
                MaxDeliveryCount = 50,
                MaxSizeInMB = 5120,
                Path = errorQueueName
            };
            managementClient.Setup(x => x.CreateQueueAsync(errorQueueDescription, It.IsAny<CancellationToken>())).ReturnsAsync(errorQueueDescription);

            managementClientFactory.Setup(x => x.GetManagementClient()).Returns(managementClient.Object);

            // Act
            listener = new JobBatchCommunicationListener(EndpointName, errorQueueName, logger.Object,
                containerScopeFactory.Object, telemetry.Object, messageDeserializer.Object,
                applicationMessageModifier.Object, managementClientFactory.Object, serviceBusClientFactory.Object);
            listener.OpenAsync(CancellationToken.None);

            // Assert
            managementClient.Verify(x => x.QueueExistsAsync(errorQueueName, It.IsAny<CancellationToken>()), Times.Once());
        }
        
        [Test]
        public void When_OpenAsync_is_called_messaging_entity_exception_is_logged()
        {
            // Arrange
            var errorQueueName = "CombinedErrorsQueue";
            managementClient.Setup(x => x.QueueExistsAsync(EndpointName, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            managementClient.Setup(x => x.QueueExistsAsync(errorQueueName, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            var queueDescription = new QueueDescription(EndpointName)
            {
                DefaultMessageTimeToLive = TimeSpan.FromDays(7),
                EnableDeadLetteringOnMessageExpiration = true,
                LockDuration = TimeSpan.FromMinutes(5),
                MaxDeliveryCount = 50,
                MaxSizeInMB = 5120,
                Path = EndpointName
            };
            managementClient.Setup(x => x.CreateQueueAsync(queueDescription, It.IsAny<CancellationToken>())).ReturnsAsync(queueDescription);
            var errorQueueDescription = new QueueDescription($@"{EndpointName}-Errors")
            {
                DefaultMessageTimeToLive = TimeSpan.FromDays(7),
                EnableDeadLetteringOnMessageExpiration = true,
                LockDuration = TimeSpan.FromMinutes(5),
                MaxDeliveryCount = 50,
                MaxSizeInMB = 5120,
                Path = errorQueueName
            };
            managementClient.Setup(x => x.CreateQueueAsync(errorQueueDescription, It.IsAny<CancellationToken>()))
                .Throws(new MessagingEntityAlreadyExistsException("Already exists"));

            managementClientFactory.Setup(x => x.GetManagementClient()).Returns(managementClient.Object);

            // Act
            listener = new JobBatchCommunicationListener(EndpointName, errorQueueName, logger.Object,
                containerScopeFactory.Object, telemetry.Object, messageDeserializer.Object,
                applicationMessageModifier.Object, managementClientFactory.Object, serviceBusClientFactory.Object);
            listener.OpenAsync(CancellationToken.None);

            // Assert
            logger.Verify(x => x.LogInfo(It.Is<string>(x => x.StartsWith("Queue already exists:", StringComparison.InvariantCulture))
                , It.IsAny<object[]>(), It.IsAny<long>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()), Times.Once());
        }
        
        [Test]
        public void When_OpenAsync_is_called_unexpected_exception_is_logged()
        {
            // Arrange
            var errorQueueName = "CombinedErrorsQueue";
            managementClient.Setup(x => x.QueueExistsAsync(EndpointName, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            managementClient.Setup(x => x.QueueExistsAsync(errorQueueName, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            var queueDescription = new QueueDescription(EndpointName)
            {
                DefaultMessageTimeToLive = TimeSpan.FromDays(7),
                EnableDeadLetteringOnMessageExpiration = true,
                LockDuration = TimeSpan.FromMinutes(5),
                MaxDeliveryCount = 50,
                MaxSizeInMB = 5120,
                Path = EndpointName
            };
            managementClient.Setup(x => x.CreateQueueAsync(queueDescription, It.IsAny<CancellationToken>())).ReturnsAsync(queueDescription);
            var errorQueueDescription = new QueueDescription($@"{EndpointName}-Errors")
            {
                DefaultMessageTimeToLive = TimeSpan.FromDays(7),
                EnableDeadLetteringOnMessageExpiration = true,
                LockDuration = TimeSpan.FromMinutes(5),
                MaxDeliveryCount = 50,
                MaxSizeInMB = 5120,
                Path = errorQueueName
            };
            managementClient.Setup(x => x.CreateQueueAsync(errorQueueDescription, It.IsAny<CancellationToken>()))
                .Throws(new InvalidOperationException("Unexpected error"));

            managementClientFactory.Setup(x => x.GetManagementClient()).Returns(managementClient.Object);

            // Act
            listener = new JobBatchCommunicationListener(EndpointName, errorQueueName, logger.Object,
                containerScopeFactory.Object, telemetry.Object, messageDeserializer.Object,
                applicationMessageModifier.Object, managementClientFactory.Object, serviceBusClientFactory.Object);
            listener.OpenAsync(CancellationToken.None);

            // Assert
            logger.Verify(x => x.LogFatal(It.Is<string>(x => x.StartsWith("Error ensuring queue:", StringComparison.InvariantCulture))
                , It.IsAny<InvalidOperationException>(), It.IsAny<object[]>(), It.IsAny<long>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()), Times.Once());
        }

        [Test]
        public async Task Listener_receives_single_message()
        {
            // Arrange
            SetupServiceBusQueues();

            var serviceBusClient = new Mock<ServiceBusClient>();
            var messageReceiver = new Mock<ServiceBusReceiver>();
            var message = new RecordEarningsJob();
            var messages = new List<ServiceBusReceivedMessage>
            {
                ServiceBusModelFactory.ServiceBusReceivedMessage(BinaryData.FromObjectAsJson(message), correlationId: Guid.NewGuid().ToString(), lockTokenGuid: Guid.NewGuid())
            };

            messageReceiver.Setup(x =>
                    x.ReceiveMessagesAsync(It.IsAny<int>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(messages);
            serviceBusClient.Setup(x => x.CreateReceiver(It.IsAny<string>())).Returns(messageReceiver.Object);
            serviceBusClientFactory.Setup(x => x.GetServiceBusClient()).Returns(serviceBusClient.Object);

            messageDeserializer.Setup(x => x.DeserializeMessage(It.IsAny<ServiceBusReceivedMessage>()))
                .Returns(message);
            applicationMessageModifier.Setup(x => x.Modify(It.IsAny<RecordEarningsJob>())).Returns(message);

            // Act
            var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(5)); //CancellationTokenSource(TimeSpan.FromMilliseconds(100));
            listener = new JobBatchCommunicationListener(EndpointName, null, logger.Object,
                containerScopeFactory.Object, telemetry.Object, messageDeserializer.Object,
                applicationMessageModifier.Object, managementClientFactory.Object, serviceBusClientFactory.Object);
            await listener.OpenAsync(cancellationTokenSource.Token);
            
            // Assert
            messageReceiver.Verify(x => x.ReceiveMessagesAsync(200, It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
            messageReceiver.Verify(x => x.CompleteMessageAsync(It.Is<ServiceBusReceivedMessage>(x => x.LockToken == messages[0].LockToken),
                It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        }

        [Test]
        public async Task Listener_retries_if_no_messages_received()
        {
            // Arrange
            SetupServiceBusQueues();

            var serviceBusClient = new Mock<ServiceBusClient>();
            var messageReceiver = new Mock<ServiceBusReceiver>();
            var message = new RecordEarningsJob();
            var messages = new List<ServiceBusReceivedMessage>
            {
                ServiceBusModelFactory.ServiceBusReceivedMessage(BinaryData.FromObjectAsJson(message), correlationId: Guid.NewGuid().ToString())
            };
            messageReceiver.SetupSequence(x =>
                    x.ReceiveMessagesAsync(It.IsAny<int>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ServiceBusReceivedMessage>())
                .ReturnsAsync(messages);
            messageReceiver.Setup(x =>
                    x.ReceiveMessagesAsync(It.IsAny<int>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(messages);
            serviceBusClient.Setup(x => x.CreateReceiver(It.IsAny<string>())).Returns(messageReceiver.Object);
            serviceBusClientFactory.Setup(x => x.GetServiceBusClient()).Returns(serviceBusClient.Object);

            messageDeserializer.Setup(x => x.DeserializeMessage(It.IsAny<ServiceBusReceivedMessage>()))
                .Returns(message);
            applicationMessageModifier.Setup(x => x.Modify(It.IsAny<RecordEarningsJob>())).Returns(message);

            // Act
            var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
            listener = new JobBatchCommunicationListener(EndpointName, null, logger.Object,
                containerScopeFactory.Object, telemetry.Object, messageDeserializer.Object,
                applicationMessageModifier.Object, managementClientFactory.Object, serviceBusClientFactory.Object);
            await listener.OpenAsync(cancellationTokenSource.Token);

            // Assert
            messageReceiver.Verify(x => x.ReceiveMessagesAsync(200, It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        }

        [Test]
        public async Task Listener_handles_errors_in_messages_batch()
        {
            // Arrange
            SetupServiceBusQueues();

            var serviceBusClient = new Mock<ServiceBusClient>();
            var messageReceiver = new Mock<ServiceBusReceiver>();
            var message = new RecordEarningsJob();

            var invalidMessage = ServiceBusModelFactory.ServiceBusReceivedMessage(null, correlationId: Guid.NewGuid().ToString());

            var messages = new List<ServiceBusReceivedMessage>
            {
                invalidMessage,
                ServiceBusModelFactory.ServiceBusReceivedMessage(BinaryData.FromObjectAsJson(message), correlationId: Guid.NewGuid().ToString()),
                ServiceBusModelFactory.ServiceBusReceivedMessage(BinaryData.FromObjectAsJson(message), correlationId: Guid.NewGuid().ToString())
            };
            messageReceiver.Setup(x =>
                    x.ReceiveMessagesAsync(It.IsAny<int>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(messages);
            messageReceiver.Setup(x => x.AbandonMessageAsync(It.IsAny<ServiceBusReceivedMessage>(),
                It.IsAny<IDictionary<string, object>>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            serviceBusClient.Setup(x => x.CreateReceiver(It.IsAny<string>())).Returns(messageReceiver.Object);
            serviceBusClientFactory.Setup(x => x.GetServiceBusClient()).Returns(serviceBusClient.Object);

            messageDeserializer.Setup(x => x.DeserializeMessage(It.IsAny<ServiceBusReceivedMessage>()))
                .Returns(message);
            applicationMessageModifier.Setup(x => x.Modify(It.IsAny<RecordEarningsJob>())).Returns(message);

            // Act
            var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
            listener = new JobBatchCommunicationListener(EndpointName, null, logger.Object,
                containerScopeFactory.Object, telemetry.Object, messageDeserializer.Object,
                applicationMessageModifier.Object, managementClientFactory.Object, serviceBusClientFactory.Object);
            await listener.OpenAsync(cancellationTokenSource.Token);

            // Assert
            messageReceiver.Verify(x => x.ReceiveMessagesAsync(200, It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
            logger.Verify(x => x.LogError(It.Is<string>(x => x.StartsWith("Error in JobBatchCommunicationListener", StringComparison.InvariantCulture))
                , It.IsAny<Exception>(), It.IsAny<object[]>(), It.IsAny<long>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>())
                , Times.AtLeast(2));
            messageReceiver.Verify(x => x.AbandonMessageAsync(It.IsAny<ServiceBusReceivedMessage>(), It.IsAny<IDictionary<string, object>>(), It.IsAny<CancellationToken>()),
                Times.AtLeastOnce);
        }

        [Test]
        public async Task Listener_retries_failed_messages_in_batch()
        {
            // Arrange
            SetupServiceBusQueues();

            var serviceBusClient = new Mock<ServiceBusClient>();
            var messageReceiver = new Mock<ServiceBusReceiver>();
            var message = new RecordEarningsJob();

            var invalidMessage = ServiceBusModelFactory.ServiceBusReceivedMessage(null, correlationId: Guid.NewGuid().ToString(), lockTokenGuid: Guid.NewGuid());

            var messages = new List<ServiceBusReceivedMessage>
            {
                invalidMessage,
                ServiceBusModelFactory.ServiceBusReceivedMessage(BinaryData.FromObjectAsJson(message), correlationId: Guid.NewGuid().ToString()),
                ServiceBusModelFactory.ServiceBusReceivedMessage(BinaryData.FromObjectAsJson(message), correlationId: Guid.NewGuid().ToString())
            };
            messageReceiver.Setup(x =>
                    x.ReceiveMessagesAsync(It.IsAny<int>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(messages);
            messageReceiver.Setup(x => x.AbandonMessageAsync(It.IsAny<ServiceBusReceivedMessage>(),
                It.IsAny<IDictionary<string, object>>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            serviceBusClient.Setup(x => x.CreateReceiver(It.IsAny<string>())).Returns(messageReceiver.Object);
            serviceBusClientFactory.Setup(x => x.GetServiceBusClient()).Returns(serviceBusClient.Object);

            messageDeserializer.Setup(x => x.DeserializeMessage(It.IsAny<ServiceBusReceivedMessage>()))
                .Returns(message);
            applicationMessageModifier.Setup(x => x.Modify(It.IsAny<RecordEarningsJob>())).Returns(message);

            // Act
            var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
            listener = new JobBatchCommunicationListener(EndpointName, null, logger.Object,
                containerScopeFactory.Object, telemetry.Object, messageDeserializer.Object,
                applicationMessageModifier.Object, managementClientFactory.Object, serviceBusClientFactory.Object);
            await listener.OpenAsync(cancellationTokenSource.Token);

            // Assert
            messageReceiver.Verify(x => x.AbandonMessageAsync(It.Is<ServiceBusReceivedMessage>(x => x.LockToken == invalidMessage.LockToken), 
                    It.IsAny<IDictionary<string, object>>(), It.IsAny<CancellationToken>()), Times.AtLeast(2));
        }

        [Test]
        public async Task Listener_retries_if_exception_thrown_while_processing()
        {
            // Arrange
            SetupServiceBusQueues();

            var serviceBusClient = new Mock<ServiceBusClient>();
            var messageReceiver = new Mock<ServiceBusReceiver>();
            var message = new RecordEarningsJob();

            var invalidMessage = ServiceBusModelFactory.ServiceBusReceivedMessage(null, correlationId: Guid.NewGuid().ToString(), lockTokenGuid: Guid.NewGuid());

            var messages = new List<ServiceBusReceivedMessage>
            {
                invalidMessage,
                ServiceBusModelFactory.ServiceBusReceivedMessage(BinaryData.FromObjectAsJson(message), correlationId: Guid.NewGuid().ToString()),
                ServiceBusModelFactory.ServiceBusReceivedMessage(BinaryData.FromObjectAsJson(message), correlationId: Guid.NewGuid().ToString())
            };
            messageReceiver.Setup(x =>
                    x.ReceiveMessagesAsync(It.IsAny<int>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(messages);
            messageReceiver.Setup(x => x.AbandonMessageAsync(It.IsAny<ServiceBusReceivedMessage>(),
                It.IsAny<IDictionary<string, object>>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            serviceBusClient.Setup(x => x.CreateReceiver(It.IsAny<string>())).Returns(messageReceiver.Object);
            serviceBusClientFactory.Setup(x => x.GetServiceBusClient()).Returns(serviceBusClient.Object);

            messageDeserializer.SetupSequence(x => x.DeserializeMessage(It.IsAny<ServiceBusReceivedMessage>()))
                .Throws(new Exception("Deserializer exception"))
                .Returns(message);
            applicationMessageModifier.Setup(x => x.Modify(It.IsAny<RecordEarningsJob>())).Returns(message);

            // Act
            var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
            listener = new JobBatchCommunicationListener(EndpointName, null, logger.Object,
                containerScopeFactory.Object, telemetry.Object, messageDeserializer.Object,
                applicationMessageModifier.Object, managementClientFactory.Object, serviceBusClientFactory.Object);
            await listener.OpenAsync(cancellationTokenSource.Token);

            // Assert
            messageReceiver.Verify(x => x.ReceiveMessagesAsync(200, It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()), Times.AtLeastOnce);
            logger.Verify(x => x.LogError(It.Is<string>(x => x.Contains("Error deserializing the message")), It.IsAny<Exception>(),
                It.IsAny<object[]>(), It.IsAny<long>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<int>()), Times.Once());
        }

        [Test]
        public async Task Listener_puts_message_on_dead_letter_if_error_when_deserializing()
        {
            // Arrange
            SetupServiceBusQueues();

            var serviceBusClient = new Mock<ServiceBusClient>();
            var messageReceiver = new Mock<ServiceBusReceiver>();
            var message = new RecordEarningsJob();
            var messages = new List<ServiceBusReceivedMessage>
            {
                ServiceBusModelFactory.ServiceBusReceivedMessage(BinaryData.FromObjectAsJson(message), correlationId: Guid.NewGuid().ToString())
            };

            messageReceiver.Setup(x =>
                    x.ReceiveMessagesAsync(It.IsAny<int>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(messages);
            serviceBusClient.Setup(x => x.CreateReceiver(It.IsAny<string>())).Returns(messageReceiver.Object);
            serviceBusClientFactory.Setup(x => x.GetServiceBusClient()).Returns(serviceBusClient.Object);

            messageDeserializer.Setup(x => x.DeserializeMessage(It.IsAny<ServiceBusReceivedMessage>()))
                .Throws(new Exception("Deserialization error"));
            applicationMessageModifier.Setup(x => x.Modify(It.IsAny<RecordEarningsJob>())).Returns(message);

            // Act
            var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
            listener = new JobBatchCommunicationListener(EndpointName, null, logger.Object,
                containerScopeFactory.Object, telemetry.Object, messageDeserializer.Object,
                applicationMessageModifier.Object, managementClientFactory.Object, serviceBusClientFactory.Object);
            await listener.OpenAsync(cancellationTokenSource.Token);

            // Assert
            messageReceiver.Verify(x => x.ReceiveMessagesAsync(200, It.IsAny<TimeSpan>(), 
                It.IsAny<CancellationToken>()), Times.AtLeastOnce);
            messageReceiver.Verify(x => x.DeadLetterMessageAsync(It.IsAny<ServiceBusReceivedMessage>(), 
                It.IsAny<IDictionary<string,object>>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce());
            logger.Verify(x => x.LogError(It.Is<string>(x => x.Contains("Error deserializing the message")), It.IsAny<Exception>(), 
                It.IsAny<object[]>(), It.IsAny<long>(), It.IsAny<string>(), It.IsAny<string>(), 
                It.IsAny<int>()), Times.AtLeastOnce());
        }

        [Test]
        public async Task Listener_processes_messages_grouped_by_job_id()
        {

        }

        private void SetupServiceBusQueues()
        {
            managementClient.Setup(x => x.QueueExistsAsync(EndpointName, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            managementClient.Setup(x => x.QueueExistsAsync($@"{EndpointName}-Errors", It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            var queueDescription = new QueueDescription(EndpointName)
            {
                DefaultMessageTimeToLive = TimeSpan.FromDays(7),
                EnableDeadLetteringOnMessageExpiration = true,
                LockDuration = TimeSpan.FromMinutes(5),
                MaxDeliveryCount = 50,
                MaxSizeInMB = 5120,
                Path = EndpointName
            };
            managementClient.Setup(x => x.CreateQueueAsync(queueDescription, It.IsAny<CancellationToken>())).ReturnsAsync(queueDescription);
            var errorQueueDescription = new QueueDescription($@"{EndpointName}-Errors")
            {
                DefaultMessageTimeToLive = TimeSpan.FromDays(7),
                EnableDeadLetteringOnMessageExpiration = true,
                LockDuration = TimeSpan.FromMinutes(5),
                MaxDeliveryCount = 50,
                MaxSizeInMB = 5120,
                Path = $@"{EndpointName}-Errors"
            };
            managementClient.Setup(x => x.CreateQueueAsync(errorQueueDescription, It.IsAny<CancellationToken>())).ReturnsAsync(errorQueueDescription);

            managementClientFactory.Setup(x => x.GetManagementClient()).Returns(managementClient.Object);
        }

    }

    public class TestUnMappedMessage
    {
        public string Foo { get; set; }
        public string Bar { get; set; }
    }
}
