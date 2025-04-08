using Azure.Messaging.ServiceBus;
using NUnit.Framework;

[TestFixture]
public class QueuesStatusTests : CommandTestFixture
{
    [Test]
    public async Task ReturnsZeroWhenNoQueuesHaveDLQMessages()
    {
        await ClearAllTestQueues();
        var output = await ExecuteCommandAndExpectSuccess($"queues");

        Assert.That(output, Contains.Substring("No DLQ messages found"));
    }
    
    [Test]
    public async Task ReturnsNonZeroWhenAtLeastOneQueueHaveDLQMessages()
    {
        await ClearAllTestQueues();
        await CreateQueueWithDLQMessage(TestQueueName);
        
        var result = await ExecuteCommand($"queues");
        
        Assert.That(result.ExitCode, Is.Not.Zero);
        Assert.That(result.Output, Contains.Substring(TestQueueName));
    }
    
    async Task CreateQueueWithDLQMessage(string queueName)
    {
        await DeleteQueue(queueName);
        await AdministrationClient.CreateQueueAsync(queueName);
        
        await using var sender = ServiceBusClient.CreateSender(queueName);
            
        await sender.SendMessageAsync(new ServiceBusMessage());

        await using var receiver = ServiceBusClient.CreateReceiver(queueName);

        var message = await receiver.ReceiveMessageAsync(cancellationToken: TestTimeoutCancellationToken);
        
        await receiver.DeadLetterMessageAsync(message,"Some reason", "Some description", TestTimeoutCancellationToken);
    }
}