using NServiceBus;
using SFA.DAS.Payments.Messages.Common;

namespace SFA.DAS.Payments.Monitoring.Tests.Specs.Messages
{
    public class TestMonitoredEvent : IEvent, IMonitoredMessage
    {
        public long JobId { get; set; }
    }
}
