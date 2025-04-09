using Azure.Messaging.ServiceBus;
using NUnit.Framework;

[TestFixture]
public class ShowQueueTests : CommandTestFixture
{
    [Test]
    public async Task ThrowsIfQueueDoesNotExist()
    {
        var result = await ExecuteCommand($"queue {TestQueueName}");

        Assert.That(result.ExitCode, Is.Not.Zero);
        Assert.That(result.Output, Contains.Substring(TestQueueName));
    }
    
    [Test]
    public async Task ListDLQMessageDetailsWhenMessagesExistsInDLQ()
    {
        await CreateQueueWithDLQMessage(TestQueueName);

        var result = await ExecuteCommand($"queue {TestQueueName}");

        Assert.That(result.ExitCode, Is.Not.Zero);
        Assert.That(result.Output, Contains.Substring(TestQueueName));
    }
}