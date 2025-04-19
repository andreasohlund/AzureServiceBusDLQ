using Azure.Messaging.ServiceBus;
using NUnit.Framework;
using NUnit.Framework.Legacy;

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
    [TestCaseSource(nameof(TransformationVerifications))]
    public async Task MoveDLQMessagesInQueue(TransformationVerification transformationVerification)
    {
        var targetName = TestQueueName + "-target";
        await CreateQueue(targetName);

        var testMessage1 = new ServiceBusMessage
        {
            MessageId = Guid.NewGuid().ToString(),
        };

        transformationVerification.AdjustMessage(testMessage1);
        
        await CreateQueueWithDLQMessage(TestQueueName, testMessage1);

        var testMessage2 = new ServiceBusMessage
        {
            MessageId = Guid.NewGuid().ToString(),
        };

        transformationVerification.AdjustMessage(testMessage2);
        
        await AddDLQMessage(TestQueueName, testMessage2);
        var result = await ExecuteCommand($"move-dlq-messages {TestQueueName} {targetName} -t {transformationVerification.OptionKey}");

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

        transformationVerification.AssertMovedMessage(TestQueueName, testMessage1, retriedMessage1);

        var retriedMessage2 = await receiver.ReceiveMessageAsync(cancellationToken: TestTimeoutCancellationToken);

        Assert.That(retriedMessage2.MessageId, Is.EqualTo(retriedMessage2.MessageId));
        transformationVerification.AssertMovedMessage(TestQueueName, testMessage2, retriedMessage2);
    }
    
    public static IEnumerable<TestCaseData> TransformationVerifications()
    {
        return [new TestCaseData(new DefaultTransformationVerification()),new TestCaseData(new NoneTransformationVerification()),new TestCaseData(new NServiceBusTransformationVerification())];
    }
    
    public abstract class TransformationVerification
    {
        public abstract void AssertMovedMessage(string sourceQueue, ServiceBusMessage message, ServiceBusReceivedMessage movedMessage);
        public abstract string OptionKey { get; }

        public virtual void AdjustMessage(ServiceBusMessage message)
        {
        }
    }

    class DefaultTransformationVerification : TransformationVerification
    {
        public override void AssertMovedMessage(string sourceQueue, ServiceBusMessage message, ServiceBusReceivedMessage movedMessage)
        {
            Assert.That(movedMessage.ApplicationProperties["x-asb-dlq-source-queue"], Is.EqualTo(sourceQueue));
            Assert.That(movedMessage.ApplicationProperties["x-asb-dlq-reason"], Is.EqualTo("Some reason"));
            Assert.That(movedMessage.ApplicationProperties["x-asb-dlq-description"], Is.EqualTo("Some description"));
        }

        public override string OptionKey => "default";
    }
    
    class NoneTransformationVerification : TransformationVerification
    {
        public override void AssertMovedMessage(string sourceQueue, ServiceBusMessage message, ServiceBusReceivedMessage movedMessage)
        {
           CollectionAssert.AreEquivalent(movedMessage.ApplicationProperties, message.ApplicationProperties);
        }

        public override string OptionKey => "none";
    }

    class NServiceBusTransformationVerification : TransformationVerification
    {
        public override void AdjustMessage(ServiceBusMessage message)
        {
            message.ApplicationProperties[NServiceBus.Headers.MessageId] = message.MessageId;
        }

        public override void AssertMovedMessage(string sourceQueue, ServiceBusMessage message, ServiceBusReceivedMessage movedMessage)
        {
            Assert.That(movedMessage.ApplicationProperties[NServiceBus.Headers.FailedQ], Is.EqualTo(sourceQueue));
            Assert.That(movedMessage.ApplicationProperties[NServiceBus.Headers.ExceptionType], Is.EqualTo("Some reason"));
            Assert.That(movedMessage.ApplicationProperties[NServiceBus.Headers.Message], Is.EqualTo("Some description"));
            Assert.That(movedMessage.ApplicationProperties[NServiceBus.Headers.MessageId], Is.EqualTo(message.MessageId));
        }

        public override string OptionKey => "nservicebus";
    }
}