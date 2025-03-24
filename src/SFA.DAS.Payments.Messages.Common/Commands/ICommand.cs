using System;

namespace SFA.DAS.Payments.Messages.Common.Commands
{
    public interface ICommand: IPaymentsMessage
    {
        Guid CommandId { get; }
    }
}