using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Amqp.Framing;
using NUnit.Framework;

[TestFixture]
public class MoveQueueTests : CommandTestFixture
{
    [Test]
    public async Task ThrowsIfSourceQueueDoesNotExist()
    {
        var targetName = TestQueueName + "-target";
        await CreateQueue(TestQueueName);

        var result = await ExecuteCommand($"move-dlq-messages source-queue {targetName}");

        Assert.That(result.ExitCode, Is.Not.Zero);
    }

    [Test]
    public async Task ThrowsIfTargetQueueDoesNotExist()
    {
        await CreateQueue(TestQueueName);

        var result = await ExecuteCommand($"move-dlq-messages {TestQueueName} target-queue");

        Assert.That(result.ExitCode, Is.Not.Zero);
    }

    [Test]
    public async Task ReturnZeroWhenNoDlqMessagesExists()
    {
        var targetName = TestQueueName + "-target";
        await CreateQueue(TestQueueName);
        await CreateQueue(targetName);

        var result = await ExecuteCommand($"move-dlq-messages {TestQueueName} {targetName}");

        Assert.That(result.ExitCode, Is.Zero);
    }

    [Test]
    public async Task MoveDLQMessagesInQueue()
    {
        var targetName = TestQueueName + "-target";
        await CreateQueue(targetName);

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
        var result = await ExecuteCommand($"move-dlq-messages {TestQueueName} {targetName}");

        Assert.That(result.ExitCode, Is.Zero);

        await using var receiver = ServiceBusClient.CreateReceiver(targetName, new ServiceBusReceiverOptions
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