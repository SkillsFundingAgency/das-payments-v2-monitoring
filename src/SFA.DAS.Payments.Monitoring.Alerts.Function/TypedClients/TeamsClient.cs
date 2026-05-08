using System;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace SFA.DAS.Payments.Monitoring.Alerts.Function.TypedClients
{
    public class TeamsClient : ITeamsClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<TeamsClient> _logger;

        public TeamsClient(HttpClient httpClient, Ilogger<TeamsClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;

        }

        public async Task PostAsJsonAsync(string teamsWebhookUrl, string jsonPayload)
        {
            if (string.IsNullOrEmpty(teamsWebhookUrl))
            {
                throw new ArgumentNullException("URL is empty", nameof(teamsWebhookUrl));
            }

            using (var context = new StringContent(jsonPayload, Encoding.UTF8, "application/json"))
            {
                var response = await _httpClient.PostAsync(teamsWebhookUrl, content);
            }
            
            if (WebResponse.IsSuccessStatusCode)
            {
                _logger.LogInformation("Sent successfully");
                return;
            }

            if (jsonPayload == null) 
            {
                throw new ArgumentNullException(nameof(jsonPayload));
            }

            var response = await _httpClient.PostAsync(teamsWebhookUrl, requestContent);

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                throw new ArgumentException($"STeams API returned HTTP 400 Bad Request : {responseContent}");
            }

            response.EnsureSuccessStatusCode();

            return response;
        }
    }
}