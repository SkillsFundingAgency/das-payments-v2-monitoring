﻿using Microsoft.ServiceFabric.Services.Remoting;
using Microsoft.ServiceFabric.Services.Remoting.FabricTransport;

[assembly: FabricTransportServiceRemotingProvider(RemotingListenerVersion = RemotingListenerVersion.V2_1, RemotingClientVersion = RemotingClientVersion.V2_1)]
namespace SFA.DAS.Payments.Monitoring.Jobs.JobService.Interfaces
{
    public interface IJobService: IService
    {
        //Task RecordEarningsJob(RecordEarningsJob message, CancellationToken cancellationToken);
        //Task RecordJobAdditionalMessages(RecordJobAdditionalMessages message, CancellationToken cancellationToken);
        //Task RecordJobMessageProcessingStatus(RecordJobMessageProcessingStatus message, CancellationToken cancellationToken);
        //Task RecordJobMessageProcessingStartedStatus(RecordStartedProcessingJobMessages message,
        //    CancellationToken cancellationToken);
    }
}