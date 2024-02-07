using System;

namespace SFA.DAS.Payments.Monitoring.Jobs.DataMessages.Commands
{
    public class GeneratedMessage
    {
        public DateTimeOffset StartTime { get; set; }
        public Guid MessageId { get; set; }
        public string MessageName { get; set; }
    }
}