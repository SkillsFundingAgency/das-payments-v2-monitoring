namespace SFA.DAS.Payments.Messages.Common
{
    public interface IJobMessage: IPaymentsMessage
    {
        long JobId { get; }
    }
}