using Azure.Messaging.ServiceBus;
using NUnit.Framework;

[TestFixture]
public class ShowQueueTests : CommandTestFixture
{
    [Test]
    public async Task ThrowsIfQueueDoesNotExist()
    {
        var result = await ExecuteCommand($"queue does-not-exist");

        Assert.That(result.ExitCode, Is.Not.Zero);
    }

    [Test]
    public async Task ReturnZeroWhenNoDlqMessagesExists()
    {
        await CreateQueue(TestQueueName);

        var result = await ExecuteCommand($"queue {TestQueueName}");

        Assert.That(result.ExitCode, Is.Zero);
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