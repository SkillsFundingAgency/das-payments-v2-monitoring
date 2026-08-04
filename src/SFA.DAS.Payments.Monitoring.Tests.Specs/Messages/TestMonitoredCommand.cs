using System;
using NServiceBus;
using SFA.DAS.Payments.Messages.Common;

namespace SFA.DAS.Payments.Monitoring.Tests.Specs.Messages
{
    public class TestMonitoredCommand : ICommand, IMonitoredMessage
    {
        public Guid CommandId { get; set; } = Guid.NewGuid();
        public long JobId { get; set; }
        public bool ShouldFail { get; set; }
    }
}
