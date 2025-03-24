namespace SFA.DAS.Payments.Messages.Common
{
    public interface ITransferAccountIdsMessage
    {
        long? AccountId { get; set; }
        long? TransferSenderAccountId { get; set; }
    }
}