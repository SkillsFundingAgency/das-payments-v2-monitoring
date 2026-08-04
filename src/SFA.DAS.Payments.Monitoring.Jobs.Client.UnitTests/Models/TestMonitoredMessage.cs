using SFA.DAS.Payments.Messages.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SFA.DAS.Payments.Messages.Common.Events;

namespace SFA.DAS.Payments.Monitoring.Jobs.Client.UnitTests.Models
{
    public class TestMonitoredMessage : IMonitoredMessage, IEvent
    {
        public long JobId { get; set; }
        public Guid EventId { get; set; }
        public DateTimeOffset EventTime { get; set; }
    }
}
