using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.ServiceBus.Management;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using SFA.DAS.Payments.Application.Infrastructure.Ioc;
using SFA.DAS.Payments.Application.Infrastructure.Logging;
using SFA.DAS.Payments.Application.Infrastructure.Telemetry;
using SFA.DAS.Payments.Application.Messaging;
using SFA.DAS.Payments.Messaging.Serialization;
using SFA.DAS.Payments.Monitoring.Jobs.Messages;
using SFA.DAS.Payments.ServiceFabric.Core;
using SFA.DAS.Payments.ServiceFabric.Core.Infrastructure.UnitOfWork;

namespace SFA.DAS.Payments.Monitoring.Jobs.Application.Infrastructure.Messaging
{

    public interface IJobBatchCommunicationListener : ICommunicationListener
    {
        string EndpointName { get; set; }
    }

    public class JobBatchCommunicationListener : IJobBatchCommunicationListener
    {
        private readonly IPaymentLogger logger;
        private readonly IContainerScopeFactory scopeFactory;
        private readonly ITelemetry telemetry;
        private readonly IMessageDeserializer messageDeserializer;
        private readonly IApplicationMessageModifier messageModifier;
        private readonly IManagementClientFactory managementClientFactory;
        private readonly IServiceBusClientFactory serviceBusClientFactory;
        public string EndpointName { get; set; }
        private readonly string errorQueueName;
        private CancellationToken startingCancellationToken;
        protected string TelemetryPrefix => GetType().Name;

        private const string TopicPath = "bundle-1";

        public JobBatchCommunicationListener(string endpointName, string errorQueueName, IPaymentLogger logger,
            IContainerScopeFactory scopeFactory, ITelemetry telemetry, IMessageDeserializer messageDeserializer, IApplicationMessageModifier messageModifier,
            IManagementClientFactory managementClientFactory, IServiceBusClientFactory serviceBusClientFactory)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
            this.telemetry = telemetry ?? throw new ArgumentNullException(nameof(telemetry));
            this.messageDeserializer = messageDeserializer ?? throw new ArgumentNullException(nameof(messageDeserializer));
            this.messageModifier = messageModifier ?? throw new ArgumentNullException(nameof(messageModifier));
            this.managementClientFactory = managementClientFactory ?? throw new ArgumentNullException(nameof(managementClientFactory));
            this.serviceBusClientFactory = serviceBusClientFactory ?? throw new ArgumentNullException(nameof(serviceBusClientFactory));
            EndpointName = endpointName ?? throw new ArgumentNullException(nameof(endpointName));
            this.errorQueueName = errorQueueName ?? endpointName + "-Errors";
        }

        public Task<string> OpenAsync(CancellationToken cancellationToken)
        {
            startingCancellationToken = cancellationToken;
            _ = ListenForMessages(cancellationToken);
            return Task.FromResult(EndpointName);
        }

        protected virtual async Task ListenForMessages(CancellationToken cancellationToken)
        {
            await EnsureQueue(EndpointName).ConfigureAwait(false);
            await EnsureQueue(errorQueueName).ConfigureAwait(false);
            try
            {
                await Listen(cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogFatal($"Encountered fatal error. Error: {ex.Message}", ex);
            }
        }

        private async Task<List<(Object Message, BatchMessageReceiver Receiver, ServiceBusReceivedMessage ReceivedMessage)>> ReceiveMessages(BatchMessageReceiver messageReceiver, CancellationToken cancellationToken)
        {
            var applicationMessages = new List<(Object Message, BatchMessageReceiver Receiver, ServiceBusReceivedMessage ReceivedMessage)>();
            var messages = await messageReceiver.ReceiveMessages(200, cancellationToken).ConfigureAwait(false);
            if (!messages.Any())
                return applicationMessages;

            foreach (var message in messages)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    var applicationMessage = GetApplicationMessage(message);
                    applicationMessages.Add((applicationMessage, messageReceiver, message));
                }
                catch (Exception e)
                {
                    logger.LogError($"Error deserializing the message. Error: {e.Message}", e);
                    //TODO: should use the error queue instead of dead letter queue
                    await messageReceiver.DeadLetter(message.LockToken, CancellationToken.None)
                        .ConfigureAwait(false);
                }
            }

            return applicationMessages;
        }
        
        private async Task Listen(CancellationToken cancellationToken)
        {
            var client = serviceBusClientFactory.GetServiceBusClient();
            var messageReceivers = new List<BatchMessageReceiver>();
            messageReceivers.AddRange(Enumerable.Range(0, 3)
                .Select(i => new BatchMessageReceiver(client, EndpointName)));
            
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        var pipeLineStopwatch = Stopwatch.StartNew();
                        var receiveTimer = Stopwatch.StartNew();

                        var receiveTasks =
                            messageReceivers.Select(receiver => ReceiveMessages(receiver, cancellationToken)).ToList();
                        await Task.WhenAll(receiveTasks).ConfigureAwait(false);

                        var messageReceiverResults = receiveTasks.SelectMany(task => task.Result).ToList();
                        receiveTimer.Stop();
                        if (!messageReceiverResults.Any())
                        {
                            await Task.Delay(2000, cancellationToken);
                            continue;
                        }
                        RecordMetric("ReceiveMessages", receiveTimer.ElapsedMilliseconds, messageReceiverResults.Count);

                        var messagesByJob = messageReceiverResults
                            .GroupBy(received => GetMessageJob(received.Message),
                            received => received);

                        var stopwatch = Stopwatch.StartNew();
                        await Task.WhenAll(messagesByJob.Select(job => ProcessJob(job.Key, job.Select(received => received).ToList(), cancellationToken)));
                        stopwatch.Stop();
                        RecordProcessedAllBatchesTelemetry(stopwatch.ElapsedMilliseconds, messageReceiverResults.Count);
                        pipeLineStopwatch.Stop();
                        RecordPipelineTelemetry(pipeLineStopwatch.ElapsedMilliseconds, messageReceiverResults.Count);
                    }
                    catch (TaskCanceledException)
                    {
                        logger.LogWarning("Cancelling communication listener.");
                        return;
                    }
                    catch (OperationCanceledException)
                    {
                        logger.LogWarning("Cancelling communication listener.");
                        return;
                    }
                    catch (Exception ex)
                    {
                        logger.LogError($"Error listening for message.  Error: {ex.Message}", ex);
                    }
                }
            }
            finally
            {
                await Task.WhenAll(messageReceivers.Select(receiver => receiver.Close())).ConfigureAwait(false);
                if (!client.IsClosed)
                {
                    await client.DisposeAsync();
                }
            }
        }

        private long GetMessageJob(object message)
        {
            var jobMessage = message as IJobMessage;
            return jobMessage?.JobId ?? 0;
        }

        private async Task ProcessJob(long jobId,
            List<(object Message, BatchMessageReceiver MessageReceiver, ServiceBusReceivedMessage ReceivedMessage)> messages,
            CancellationToken cancellationToken)
        {
            try
            {
                var messagesByType = messages.GroupBy(message => message.Message.GetType(), message => message);

                //process job message types sequentially to avoid clashes on access to same reliable cache for current job 
                foreach (var messageBatch in messagesByType)
                {
                    //TODO: check if the messages have already expired before processing

                    await ProcessMessages(jobId, messageBatch.Key, messageBatch.Select(received => received).ToList(),
                        cancellationToken);
                }
                
            }
            catch (Exception e)
            {
                logger.LogError($"Error processing batch of messages with job: {jobId}. Error: {e.Message}", e);
                throw;
            }
        }
        
        private void RecordProcessedBatchTelemetry(long elapsedMilliseconds, int count, string batchType)
        {
            RecordMetric("ProcessedBatch", elapsedMilliseconds, count, (properties, metrics) => properties.Add("MessageBatchType", batchType));
        }

        private void RecordProcessedAllBatchesTelemetry(long elapsedMilliseconds, int count)
        {
            RecordMetric("ProcessedAllBatches", elapsedMilliseconds, count);
        }

        private void RecordPipelineTelemetry(long elapsedMilliseconds, int count)
        {
            RecordMetric("Pipeline", elapsedMilliseconds, count);
        }

        private void RecordMetric(string eventName, long elapsedMilliseconds, int count, Action<Dictionary<string, string>, Dictionary<string, double>> metricsAction = null)
        {
            var metrics = new Dictionary<string, double>
            {
                {TelemetryKeys.Duration, elapsedMilliseconds},
                {TelemetryKeys.Count, count}
            };
            var properties = new Dictionary<string, string>();
            metricsAction?.Invoke(properties, metrics);
            telemetry.TrackEvent($"{TelemetryPrefix}.{eventName}", properties, metrics);
        }

        private object GetApplicationMessage(ServiceBusReceivedMessage message)
        {
            var applicationMessage = DeserializeMessage(message);
            return messageModifier.Modify(applicationMessage);
        }

        private object DeserializeMessage(ServiceBusReceivedMessage message)
        {
            return messageDeserializer.DeserializeMessage(message);
        }

        protected async Task ProcessMessages(long jobId, Type groupType, List<(object Message, BatchMessageReceiver MessageReceiver, ServiceBusReceivedMessage ReceivedMessage)> messages,
            CancellationToken cancellationToken)
        {
            try
            {
                using (var containerScope = scopeFactory.CreateScope())
                {
                    var unitOfWork = containerScope.Resolve<IStateManagerUnitOfWork>();
                    try
                    {
                        await unitOfWork.Begin();
                        if (!containerScope.TryResolve(typeof(IHandleMessageBatches<>).MakeGenericType(groupType),
                                out object handler))
                        {
                            logger.LogError($"No handler found for message: {groupType.FullName}");
                            await Task.WhenAll(messages.Select(message => message.MessageReceiver.DeadLetter(message.ReceivedMessage.LockToken, CancellationToken.None)));
                            return;
                        }

                        var methodInfo = handler.GetType().GetMethod("Handle");
                        if (methodInfo == null)
                            throw new InvalidOperationException($"Handle method not found on handler: {handler.GetType().Name} for message type: {groupType.FullName}");

                        var listType = typeof(List<>).MakeGenericType(groupType);
                        var list = (IList)Activator.CreateInstance(listType);

                        messages.ForEach(message => list.Add(message.Message));

                        var handlerStopwatch = Stopwatch.StartNew();
                        await (Task)methodInfo.Invoke(handler, new object[] { list, cancellationToken });
                        RecordMetric(handler.GetType().FullName, handlerStopwatch.ElapsedMilliseconds, list.Count);

                        await unitOfWork.End();
                        await Task.WhenAll(messages.GroupBy(msg => msg.MessageReceiver).Select(group =>
                            group.Key.Complete(group.Select(msg => msg.ReceivedMessage.LockToken)))).ConfigureAwait(false);
                    }
                    catch (Exception e)
                    {
                        await unitOfWork.End(e);
                        throw;
                    }
                }
            }
            catch (Exception e)
            {
                logger.LogError($"Error in JobBatchCommunicationListener, Message Type: {messages.First().Message.GetType().Name}, Message Count: {messages.Count}, Error: {e.Message}", e);
                await Task.WhenAll(messages.Where(msg => msg.ReceivedMessage.DeliveryCount < 10).GroupBy(msg => msg.MessageReceiver).Select(group =>
                        group.Key.Abandon(group.Select(msg => msg.ReceivedMessage.LockToken)
                            .ToList())))
                    .ConfigureAwait(false);
                await RetryFailedMessages(groupType, messages.Where(msg => msg.ReceivedMessage.DeliveryCount >= 10).ToList(), cancellationToken);
            }


        }

        protected async Task RetryFailedMessages(Type groupType,
            List<(object Message, BatchMessageReceiver MessageReceiver, ServiceBusReceivedMessage ReceivedMessage)> messages,
            CancellationToken cancellationToken)
        {
            var listType = typeof(List<>).MakeGenericType(groupType);
            var list = (IList)Activator.CreateInstance(listType);
            foreach (var retryMessage in messages)
            {
                try
                {
                    using (var scope = scopeFactory.CreateScope())
                    {
                        var unitOfWork = scope.Resolve<IStateManagerUnitOfWork>();
                        try
                        {
                            await unitOfWork.Begin();
                            if (!scope.TryResolve(typeof(IHandleMessageBatches<>).MakeGenericType(groupType),
                                    out object handler))
                            {
                                logger.LogError($"No handler found for message: {groupType.FullName}");
                                await Task.WhenAll(messages.Select(message => message.MessageReceiver.DeadLetter(message.ReceivedMessage.LockToken, CancellationToken.None)));
                                return;
                            }

                            var methodInfo = handler.GetType().GetMethod("Handle");
                            if (methodInfo == null)
                                throw new InvalidOperationException($"Handle method not found on handler: {handler.GetType().Name} for message type: {groupType.FullName}");

                            list.Clear();
                            list.Add(retryMessage.Message);

                            await (Task)methodInfo.Invoke(handler, new object[] { list, cancellationToken });

                            await unitOfWork.End();
                            await retryMessage.MessageReceiver.Complete(new List<string> { retryMessage.ReceivedMessage.LockToken });
                        }
                        catch (Exception e)
                        {
                            await unitOfWork.End(e);
                            throw;
                        }


                    }
                }
                catch (Exception e)
                {
                    logger.LogError($"Error in StatelessServiceBusBatchCommunicationListener, Message Type:  {retryMessage.GetType().Name}, Error: {e.Message}.  ASB Message id: {retryMessage.ReceivedMessage.MessageId}, Message label: {retryMessage.ReceivedMessage.Subject}.", e);
                    await retryMessage.MessageReceiver.Abandon(new List<string> { retryMessage.ReceivedMessage.LockToken });
                }
            }
        }

        public Task CloseAsync(CancellationToken cancellationToken)
        {
            if (!startingCancellationToken.IsCancellationRequested)
                startingCancellationToken = cancellationToken;
            return Task.CompletedTask;
        }

        public void Abort()
        {
        }

        private async Task EnsureQueue(string queuePath)
        {
            try
            {
                var managementClient = managementClientFactory.GetManagementClient();
                if (await managementClient.QueueExistsAsync(queuePath, startingCancellationToken).ConfigureAwait(false))
                {
                    logger.LogInfo($"Queue '{queuePath}' already exists, skipping queue creation.");
                    return;
                }

                logger.LogInfo(
                    $"Creating queue '{queuePath}' with properties: TimeToLive: 7 days, Lock Duration: 5 Minutes, Max Delivery Count: 50, Max Size: 5Gb.");
                var queueDescription = new QueueDescription(queuePath)
                {
                    DefaultMessageTimeToLive = TimeSpan.FromDays(7),
                    EnableDeadLetteringOnMessageExpiration = true,
                    LockDuration = TimeSpan.FromMinutes(5),
                    MaxDeliveryCount = 50,
                    MaxSizeInMB = 5120,
                    Path = queuePath
                };

                await managementClient.CreateQueueAsync(queueDescription, startingCancellationToken).ConfigureAwait(false);
            }
            catch (MessagingEntityAlreadyExistsException ex)
            {
                logger.LogInfo($"Queue already exists: {ex.Message}. This could be because another instance of the service has already ensured the queue exists");
            }
            catch (Exception e)
            {
                logger.LogFatal($"Error ensuring queue: {e.Message}.", e);
                throw;
            }
        }
    }
}