namespace SFA.DAS.Payments.Messages.Common.Events
{
    public interface IContractType1EarningEvent : IContractTypeEarningEvent
    {
        string AgreementId { get; }
    }
}