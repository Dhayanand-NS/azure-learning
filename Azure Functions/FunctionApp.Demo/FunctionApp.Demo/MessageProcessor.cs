using FunctionApp.Demo;
using Google.Protobuf;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Collections;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace FunctionApp.Demo
{
    public class MessageProcessor
    {
        private readonly ILogger<MessageProcessor> _logger;

        public MessageProcessor(ILogger<MessageProcessor> logger)
        {
            _logger = logger;
        }
        //Registers this as an Azure Function named MessageProcessor
        [Function("MessageProcessor")]
        //Constantly watches the queue named message-queue
        //As soon as a message appears → this function fires automatically
        //Connection = "AzureWebJobsStorage" → tells it which storage account to watch(from your local.settings.json)
        public void Run([QueueTrigger("message-queue",Connection = "AzureWebJobsStorage")] string myQueueItem) //The actual message content from the queue gets injected in the myQueueItem automatically
                                                                                                               //Example: if queue has {"orderId": "123"} → myQueueItem = {"orderId": "123"}            
        {
            //Just logs the message for now
            _logger.LogInformation($"C# Queue trigger function processed: {myQueueItem}");
        }
    }
}
//Example flow of how this works in real life:

//Azure Queue "message-queue"
//        │
//        │  new message arrives
//        ▼
//MessageProcessor wakes up automatically
//        │
//        ▼
//Reads the message
//        │
//        ▼
//Do your logic (email, validate, alert)
//        │
//        ▼
//Message deleted from queue automatically