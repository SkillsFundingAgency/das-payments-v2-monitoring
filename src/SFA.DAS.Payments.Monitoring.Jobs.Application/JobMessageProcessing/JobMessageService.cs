﻿using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SFA.DAS.Payments.Application.Infrastructure.Logging;
using SFA.DAS.Payments.Application.Infrastructure.Telemetry;
using SFA.DAS.Payments.Monitoring.Jobs.DataMessages.Commands;
using SFA.DAS.Payments.Monitoring.Jobs.Model;

namespace SFA.DAS.Payments.Monitoring.Jobs.Application.JobMessageProcessing
{
    public interface IJobMessageService
    {
        Task RecordCompletedJobMessageStatus(RecordJobMessageProcessingStatus jobMessageStatus, CancellationToken cancellationToken);
    }

    public class JobMessageService : IJobMessageService
    {
        private readonly IJobStorageService jobStorageService;
        private readonly IPaymentLogger logger;
        private readonly ITelemetry telemetry;

        public JobMessageService(IJobStorageService jobStorageService, IPaymentLogger logger, ITelemetry telemetry)
        {
            this.jobStorageService = jobStorageService ?? throw new ArgumentNullException(nameof(jobStorageService));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.telemetry = telemetry ?? throw new ArgumentNullException(nameof(telemetry));
        }

        public async Task RecordCompletedJobMessageStatus(RecordJobMessageProcessingStatus jobMessageStatus, CancellationToken cancellationToken)
        {
            logger.LogVerbose($"Now storing the completed message. Message id: {jobMessageStatus.Id}, Job: {jobMessageStatus.JobId}, End time: {jobMessageStatus.EndTime}, Succeeded: {jobMessageStatus.Succeeded}");
            
            await jobStorageService.StoreCompletedMessage(new CompletedMessage
            {
                MessageId = jobMessageStatus.Id, 
                JobId = jobMessageStatus.JobId,
                CompletedTime = jobMessageStatus.EndTime, 
                Succeeded = jobMessageStatus.Succeeded
            },cancellationToken).ConfigureAwait(false);

            logger.LogVerbose($"Stored completed message. Now storing {jobMessageStatus.GeneratedMessages.Count} in progress messages generated while processing message: {jobMessageStatus.Id} for job: {jobMessageStatus.JobId}");
            
            await jobStorageService.StoreInProgressMessages(jobMessageStatus.JobId,
                jobMessageStatus.GeneratedMessages.Select(message => new InProgressMessage
                {
                    MessageId = message.MessageId, JobId = jobMessageStatus.JobId, MessageName = message.MessageName
                }).ToList(), cancellationToken).ConfigureAwait(false);

            logger.LogDebug($"Recorded completion of message processing.  Job Id: {jobMessageStatus.JobId}, Message id: {jobMessageStatus.Id}.");
        }
    }
}