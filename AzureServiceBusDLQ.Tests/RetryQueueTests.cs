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
        var testMessage1 = new ServiceBusMessage
        {
            MessageId = Guid.NewGuid().ToString(),
        };

        await CreateQueueWithDLQMessage(TestQueueName, testMessage1);

        var testMessage2 = new ServiceBusMessage
        {
            MessageId = Guid.NewGuid().ToString(),
        };

        await AddDLQMessage(TestQueueName, testMessage2);
        var result = await ExecuteCommand($"retry-queue {TestQueueName}");

        Assert.That(result.ExitCode, Is.Zero);

        await using var receiver = ServiceBusClient.CreateReceiver(TestQueueName, new ServiceBusReceiverOptions
        {
            ReceiveMode = ServiceBusReceiveMode.ReceiveAndDelete
        });

        var retriedMessage1 = await receiver.ReceiveMessageAsync(cancellationToken: TestTimeoutCancellationToken);

        Assert.That(retriedMessage1.MessageId, Is.EqualTo(testMessage1.MessageId));
        Assert.That(retriedMessage1.DeadLetterReason, Is.Null);
        Assert.That(retriedMessage1.DeadLetterErrorDescription, Is.Null);
        Assert.That(retriedMessage1.DeadLetterSource, Is.Null);

        var retriedMessage2 = await receiver.ReceiveMessageAsync(cancellationToken: TestTimeoutCancellationToken);

        Assert.That(retriedMessage2.MessageId, Is.EqualTo(retriedMessage2.MessageId));
    }
}