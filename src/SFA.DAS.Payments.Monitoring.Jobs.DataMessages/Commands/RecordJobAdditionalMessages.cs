using System.Collections.Generic;

namespace SFA.DAS.Payments.Monitoring.Jobs.DataMessages.Commands
{
    public class RecordJobAdditionalMessages : JobsCommand
    {
        public List<GeneratedMessage> GeneratedMessages { get; set; }
        public RecordJobAdditionalMessages()
        {
            GeneratedMessages = new List<GeneratedMessage>();
        }
    }
}