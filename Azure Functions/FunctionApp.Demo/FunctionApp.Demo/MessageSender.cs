using System.Net.Http;
using System.Text;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace FunctionApp.Demo
{
    public class MessageSender
    {
        private readonly ILogger<MessageSender> _logger;
        private readonly HttpClient _httpClient;

        public MessageSender(ILogger<MessageSender> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient();
        }

        [Function("MessageSender")]
        //This function wakes up every 5 seconds (based on the cron expression) and sends a POST request to the MessageReceiver function
        public async Task Run([TimerTrigger("*/5 * * * * *")] TimerInfo myTimer)
        {
            var message = $"Timer trigger function executed at: {DateTime.Now}";

            var content = new StringContent(JsonSerializer.Serialize(message),Encoding.UTF8,"application/json");

            await _httpClient.PostAsync("http://localhost:7050/api/MessageReceiver", content);

            _logger.LogInformation("Timer Function Executed");
        }
    }
}