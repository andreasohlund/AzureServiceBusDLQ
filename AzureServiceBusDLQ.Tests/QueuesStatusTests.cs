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
        await CreateQueueWithDLQMessage(TestQueueName);
        
        var result = await ExecuteCommand($"queues");
        
        Assert.That(result.ExitCode, Is.Not.Zero);
        Assert.That(result.Output, Contains.Substring(TestQueueName));
    }
    
    [Test]
    public async Task ReturnsNonZeroWhenAtLeastOneQueueHaveTDLQMessages()
    {
        await CreateQueueWithTDLQMessage(TestQueueName);
        
        var result = await ExecuteCommand($"queues");
        
        Assert.That(result.ExitCode, Is.Not.Zero);
        Assert.That(result.Output, Contains.Substring(TestQueueName));
    }
}