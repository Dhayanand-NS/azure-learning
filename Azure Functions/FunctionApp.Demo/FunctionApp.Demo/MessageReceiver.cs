using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Hosting;
using System.Net;
using static System.Net.WebRequestMethods;

namespace FunctionApp.Demo
{
    public class MessageReceiver
    {
        [Function("MessageReceiver")]
        //This function wakes up when someone sends a POST request
        //Anonymous means no API key or auth needed — anyone can call it
        public async Task<OutputType> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
        {
            //Reads whatever the client sent in the request body
            //Could be JSON, plain text, anything
            //Example: client sends { "name": "John"} → body = { "name": "John"}
            string body = await new StreamReader(req.Body).ReadToEndAsync();

            //Two things happen simultaneously when you return:
                //1.QueueMessage = body → Azure automatically drops the message into the queue
                //2.HttpResponse = 200 OK → Client gets a success response

            return new OutputType
            {
                QueueMessage = body,
                HttpResponse = req.CreateResponse(HttpStatusCode.OK)
            };
        }
    }

    public class OutputType
    {
        //[QueueOutput] tells Azure where to send the message
        //"message-queue" is the queue name in your storage account
        //Connection = "AzureWebJobsStorage" tells it which storage account to use(from your local.settings.json)
        [QueueOutput("message-queue", Connection = "AzureWebJobsStorage")]
        public string? QueueMessage { get; set; }

        public HttpResponseData? HttpResponse { get; set; }
    }
}
//Example flow of how this works in real life:

//1.Client calls:
//POST https://yourfunction.com/api/MessageReceiver
//   Body: { "orderId": "123", "product": "laptop"}

//2.Function reads body:
//   body = { "orderId": "123", "product": "laptop"}

//3.Message lands in Azure Queue:
//   [message-queue]
//   └── { "orderId": "123", "product": "laptop"}  ✅

//4.Client receives:
//HTTP 200 OK  ✅