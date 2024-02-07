using System;
using SFA.DAS.Payments.Messages.Common.Events;

namespace SFA.DAS.Payments.Monitoring.Jobs.DataMessages.Events
{
    public class PeriodEndJobFinishedEvent : IEvent
    {
        public Guid EventId { get; set; }
        public DateTimeOffset EventTime { get; set; }
        public short AcademicYear { get; set; }
        public byte CollectionPeriod { get; set; }
        public long JobId { get; set; }
        public PeriodEndJobFinishedEvent()
        {
            EventTime = DateTimeOffset.UtcNow;
            EventId = Guid.NewGuid();
        }
    }
}