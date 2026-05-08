using System.Net.Http;
using System.Threading.Tasks;

namespace SFA.DAS.Payments.Monitoring.Alerts.Function.TypedClients
{
    public interface ITeamsClient
    {
        public Task<HttpResponseMessage> PostAsJsonAsync(string teamsWebhookUrl, string jsonPayload);
    }
}