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
    [TestCaseSource(nameof(MessagingFrameworkVerifications))]
    public async Task MoveDLQMessagesInQueue(MessagingFrameworkVerification messagingFrameworkVerification)
    {
        var targetName = TestQueueName + "-target";
        await CreateQueue(targetName);

        var testMessage1 = new ServiceBusMessage
        {
            MessageId = Guid.NewGuid().ToString(),
        };

        messagingFrameworkVerification.TransformTestMessage(testMessage1);
        
        await CreateQueueWithDLQMessage(TestQueueName, testMessage1);

        var testMessage2 = new ServiceBusMessage
        {
            MessageId = Guid.NewGuid().ToString(),
        };

        messagingFrameworkVerification.TransformTestMessage(testMessage2);

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

        messagingFrameworkVerification.AssertMovedMessage(testMessage1, retriedMessage1);

        var retriedMessage2 = await receiver.ReceiveMessageAsync(cancellationToken: TestTimeoutCancellationToken);

        Assert.That(retriedMessage2.MessageId, Is.EqualTo(retriedMessage2.MessageId));
        messagingFrameworkVerification.AssertMovedMessage(testMessage2, retriedMessage2);
    }


    public static IEnumerable<TestCaseData> MessagingFrameworkVerifications()
    {
        return [new TestCaseData(new UnknownFrameworkVerification()),new TestCaseData(new NServiceBusVerification())];
    }
}

public abstract class MessagingFrameworkVerification
{
    public abstract void TransformTestMessage(ServiceBusMessage message);

    public abstract void AssertMovedMessage(ServiceBusMessage message, ServiceBusReceivedMessage movedMessage);
}

public class UnknownFrameworkVerification : MessagingFrameworkVerification
{
    public override void TransformTestMessage(ServiceBusMessage message)
    {
    }

    public override void AssertMovedMessage(ServiceBusMessage message, ServiceBusReceivedMessage movedMessage)
    {
        
    }
}

public class NServiceBusVerification : MessagingFrameworkVerification
{
    public override void TransformTestMessage(ServiceBusMessage message)
    {
    }

    public override void AssertMovedMessage(ServiceBusMessage message, ServiceBusReceivedMessage movedMessage)
    {
        
    }
}