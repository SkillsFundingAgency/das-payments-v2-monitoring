using System.Collections.Generic;
using SFA.DAS.Payments.Monitoring.Jobs.Messages.Commands;

namespace SFA.DAS.Payments.Monitoring.Jobs.Messages.Commands
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