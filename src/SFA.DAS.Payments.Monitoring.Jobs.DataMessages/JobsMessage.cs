namespace SFA.DAS.Payments.Monitoring.Jobs.DataMessages
{
    public abstract class JobsMessage : IJobMessage
    {
        public long JobId { get; set; }
    }
}