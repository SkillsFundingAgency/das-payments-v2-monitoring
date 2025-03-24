using System;

namespace SFA.DAS.Payments.Messages.Common.Commands
{
    public interface IPaymentsCommand: IJobMessage, ICommand
    {
        DateTimeOffset RequestTime { get; }
    }
}