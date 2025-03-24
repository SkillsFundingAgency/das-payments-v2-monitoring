using System;

namespace SFA.DAS.Payments.Messages.Common.Events
{
    public interface IEvent: IPaymentsMessage
    {
        Guid EventId { get; }
        DateTimeOffset EventTime { get; }
    }
}