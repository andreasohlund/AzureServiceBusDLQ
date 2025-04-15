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
    public async Task RetryDLQMessagesInQueue()
    {
        var testMessage = new ServiceBusMessage
        {
            MessageId = Guid.NewGuid().ToString(),
        };

        await CreateQueueWithDLQMessage(TestQueueName, testMessage);
        var result = await ExecuteCommand($"retry-queue {TestQueueName}");

        Assert.That(result.ExitCode, Is.Zero);

        await using var receiver = ServiceBusClient.CreateReceiver(TestQueueName);

        var retriedMessage = await receiver.ReceiveMessageAsync(cancellationToken: TestTimeoutCancellationToken);

        Assert.That(retriedMessage.MessageId, Is.EqualTo(testMessage.MessageId));
        Assert.That(retriedMessage.DeadLetterReason, Is.Null);
        Assert.That(retriedMessage.DeadLetterErrorDescription, Is.Null);
        Assert.That(retriedMessage.DeadLetterSource, Is.Null);
    }
}