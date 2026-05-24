using Azure.Messaging.ServiceBus;
using System.Diagnostics;

//I removed connection because unable to push the code with connection string to github because it violates the policy it means some secret is pushing, but you can get the connection string from the Azure portal and paste it here to run the code
string connectionString = "";

//string queueName = "azure-course-servicebus-1";
string topicName = "azure-course-topic";
string sub1Name = "sub1";


ServiceBusClient client = new ServiceBusClient(connectionString);
//ServiceBusProcessor processor = client.CreateProcessor(queueName, new ServiceBusProcessorOptions());
ServiceBusProcessor processor = client.CreateProcessor(topicName,sub1Name, new ServiceBusProcessorOptions());
async Task MessageHandler(ProcessMessageEventArgs processMessageEventArgs)
{
    string body = processMessageEventArgs.Message.Body.ToString();
    Console.WriteLine($"{body} - Subscription: {sub1Name}");
    Console.WriteLine($"{body}");
    await processMessageEventArgs.CompleteMessageAsync(processMessageEventArgs.Message);
}

Task ErrorHandler(ProcessErrorEventArgs processMessageEventArgs)
{
    Console.WriteLine(processMessageEventArgs.Exception.ToString());
    return Task.CompletedTask;
}


try
{
    processor.ProcessMessageAsync += MessageHandler;
    processor.ProcessErrorAsync += ErrorHandler;

    await processor.StartProcessingAsync();
    Console.WriteLine("Press any key to end the processing");
    Console.ReadKey();

    Console.WriteLine("\nStopping the receiver...");
    await processor.StopProcessingAsync();
    Console.WriteLine("Stopped receiving messages");
}
catch (Exception ex)    
{
    Console.WriteLine($"Exception: {ex.Message}");
}
finally
{
    await processor.DisposeAsync();
    await client.DisposeAsync();
}