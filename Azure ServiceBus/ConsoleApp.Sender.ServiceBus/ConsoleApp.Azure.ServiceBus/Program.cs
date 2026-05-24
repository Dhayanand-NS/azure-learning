using Azure.Messaging.ServiceBus;

//I removed connection because unable to push the code with connection string to github because it violates the policy it means some secret is pushing, but you can get the connection string from the Azure portal and paste it here to run the code
string connectionString = "";
//string queueName = "azure-course-servicebus-1";
string topicName = "azure-course-topic";
const int maxNumberofMessages = 3;

ServiceBusClient client = new ServiceBusClient(connectionString);
ServiceBusSender sender = client.CreateSender(topicName);
//ServiceBusSender sender = client.CreateSender(queueName);

//creating a batch of messages
using ServiceBusMessageBatch batch = await sender.CreateMessageBatchAsync();
for (int i = 1; i <= maxNumberofMessages; i++)
{
    //adds the message to the batch, if fails, it will return false and the message will not be added to the batch
    if (!batch.TryAddMessage(new ServiceBusMessage($"This a message - {i}")))
    {
        Console.WriteLine($"Message - {i} was not added to the batch");
    }
}

try
{
    await sender.SendMessagesAsync(batch);
    Console.WriteLine("Messages Sent");
}
catch (Exception ex)
{
    Console.WriteLine($"Exception: {ex.Message}");
}
finally
{
    await sender.DisposeAsync();
    await client.DisposeAsync();
}