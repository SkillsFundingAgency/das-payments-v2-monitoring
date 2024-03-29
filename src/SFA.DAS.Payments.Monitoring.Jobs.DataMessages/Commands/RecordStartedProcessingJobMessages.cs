﻿using System.Collections.Generic;

namespace SFA.DAS.Payments.Monitoring.Jobs.DataMessages.Commands
{
    public class RecordStartedProcessingJobMessages : JobsCommand, IJobMessageStatus
    {
        public List<GeneratedMessage> GeneratedMessages { get; set; }
        public RecordStartedProcessingJobMessages()
        {
            GeneratedMessages = new List<GeneratedMessage>();
        }
    }
}