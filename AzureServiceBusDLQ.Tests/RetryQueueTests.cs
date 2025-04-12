using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Amqp.Framing;
using NUnit.Framework;

[TestFixture]
public class RetryQueueTests : CommandTestFixture
{
    [Test]
    public async Task ThrowsIfQueueDoesNotExist()
    {
        var result = await ExecuteCommand($"retry-queue does-not-exist");

        Assert.That(result.ExitCode, Is.Not.Zero);
    }

    [Test]
    public async Task ReturnZeroWhenNoDlqMessagesExists()
    {
        await CreateQueue(TestQueueName);

        var result = await ExecuteCommand($"retry-queue {TestQueueName}");

        Assert.That(result.ExitCode, Is.Zero);
    }

    [Test]
    public async Task RetriesDLQMessagesInQueue()
    {
        var testMessage = new ServiceBusMessage();
        await CreateQueueWithDLQMessage(TestQueueName, testMessage);
        var result = await ExecuteCommand($"retry-queue {TestQueueName}");

        Assert.That(result.ExitCode, Is.Not.Zero);

        await using var receiver = ServiceBusClient.CreateReceiver(TestQueueName);

        var retriedMessage = await receiver.ReceiveMessageAsync(cancellationToken: TestTimeoutCancellationToken);

        Assert.That(retriedMessage.MessageId, Is.EqualTo(testMessage.MessageId));
    }
}