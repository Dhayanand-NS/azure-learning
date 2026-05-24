using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using System.Net;

namespace FunctionApp.Demo
{
    public class DurableTester
    {
        private readonly ILogger<DurableTester> _logger;

        public DurableTester(ILogger<DurableTester> logger)
        {
            _logger = logger;
        }

        // ─── ORCHESTRATOR ─────────────────────────────────────
        [Function("DurableTester")]
        public async Task<List<string>> RunOrchestrator([OrchestrationTrigger] TaskOrchestrationContext context)
        {
            var outputs = new List<string>();
            var input = context.GetInput<string>();

            // Runs one by one (chaining pattern)
            outputs.Add(await context.CallActivityAsync<string>(nameof(SayHello), "Tokyo"));
            outputs.Add(await context.CallActivityAsync<string>(nameof(SayHello), "Seattle"));
            outputs.Add(await context.CallActivityAsync<string>(nameof(SayHello), "London"));
            outputs.Add(await context.CallActivityAsync<string>(nameof(AddToQueue), input));

            return outputs;
        }

        // ─── ACTIVITY 1 ───────────────────────────────────────
        [Function(nameof(SayHello))]
        public string SayHello([ActivityTrigger] string name)
        {
            _logger.LogInformation($"Saying hello to {name}.");
            return $"Hello {name}!";
        }

        // ─── ACTIVITY 2 ───────────────────────────────────────
        [Function(nameof(AddToQueue))]
        public async Task<string> AddToQueue([ActivityTrigger] string messageToAdd)
        {
            _logger.LogInformation("Message added to Queue: {message}", messageToAdd);

            // Manually add to queue in isolated (no ICollector available)
            var queueClient = new Azure.Storage.Queues.QueueClient(Environment.GetEnvironmentVariable("AzureWebJobsStorage"),"outqueue-durable");

            await queueClient.CreateIfNotExistsAsync();
            await queueClient.SendMessageAsync(messageToAdd);

            return $"{messageToAdd} has been added";
        }

        // ─── CLIENT (HTTP TRIGGER) ────────────────────────────
        [Function("DurableTester_HttpStart")]
        public async Task<HttpResponseData> HttpStart([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req,[DurableClient] DurableTaskClient starter)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            // Start the orchestrator
            string instanceId = await starter.ScheduleNewOrchestrationInstanceAsync("DurableTester", requestBody);

            _logger.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            // Returns URLs to check status
            return await starter.CreateCheckStatusResponseAsync(req, instanceId);
        }
    }
}


//Work Flow::

//USER
//│
//│  POST http://localhost:7071/api/DurableTester_HttpStart
//│  Body: "Process this message"
//│
//▼
//╔══════════════════════════════╗
//║   HttpStart(CLIENT)         ║
//║   HttpTrigger fires          ║
//║   Reads request body         ║
//║   Starts orchestrator        ║
//║   Returns status URLs        ║
//╚══════════════════════════════╝
//│
//│  Returns instantly to user:
//│  {
//│    "id": "abc123",
//│    "statusQueryGetUri": "http://...",
//│  }
//│
//▼
//╔══════════════════════════════╗
//║   RunOrchestrator(BRAIN)    ║
//║   Receives "Process this     ║
//║   message" as input          ║
//╚══════════════════════════════╝
//│
//│  Calls activities ONE BY ONE
//│
//├──────────────────────────────────────────────────────►
//│                                                      │
//│                                              ╔═══════════════╗
//│                                              ║   SayHello    ║
//│                                              ║   "Tokyo"     ║
//│                                              ║   returns     ║
//│                                              ║ "Hello Tokyo!"║
//│                                              ╚═══════════════╝
//│◄─────────────────────────────────────────────────────┘
//│  outputs = ["Hello Tokyo!"]
//│
//├──────────────────────────────────────────────────────►
//│                                                      │
//│                                              ╔═══════════════╗
//│                                              ║   SayHello    ║
//│                                              ║   "Seattle"   ║
//│                                              ║   returns     ║
//│                                              ║"Hello Seattle"║
//│                                              ╚═══════════════╝
//│◄─────────────────────────────────────────────────────┘
//│  outputs = ["Hello Tokyo!", "Hello Seattle!"]
//│
//├──────────────────────────────────────────────────────►
//│                                                      │
//│                                              ╔═══════════════╗
//│                                              ║   SayHello    ║
//│                                              ║   "London"    ║
//│                                              ║   returns     ║
//│                                              ║"Hello London!"║
//│                                              ╚═══════════════╝
//│◄─────────────────────────────────────────────────────┘
//│  outputs = ["Hello Tokyo!", "Hello Seattle!", "Hello London!"]
//│
//├──────────────────────────────────────────────────────►
//│                                                      │
//│                                              ╔═══════════════╗
//│                                              ║  AddToQueue   ║
//│                                              ║  "Process     ║
//│                                              ║  this message"║
//│                                              ║  drops to     ║
//│                                              ║  queue ✅     ║
//│                                              ╚═══════════════╝
//│◄─────────────────────────────────────────────────────┘
//│  outputs = ["Hello Tokyo!", "Hello Seattle!", 
//│             "Hello London!", "Process this message has been added"]
//│
//▼
//╔══════════════════════════════╗
//║   Orchestration COMPLETE     ║
//║   Final output saved         ║
//║   User polls statusQueryUri  ║
//║   Gets final result          ║
//╚══════════════════════════════╝
//│
//▼
//[outqueue-durable]
//└── "Process this message"  ← sitting here for next function to pick up