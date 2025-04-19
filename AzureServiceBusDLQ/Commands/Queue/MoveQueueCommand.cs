using System.ComponentModel;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using AzureServiceBusDLQ.Infrastructure;
using Spectre.Console;
using Spectre.Console.Cli;

public class MoveQueueCommand(QueueOperations queueOperations) : CancellableAsyncCommand<MoveQueueCommand.Settings>
{
    public class Settings : BaseSettings
    {
        [Description("Queue name from which DLQ messages will be moved")]
        [CommandArgument(0, "<QueueName>")]
        public string QueueName { get; set; } = null!;

        [Description("Queue name to which messages will be moved")]
        [CommandArgument(0, "<TargetQueueName>")]
        public string TargetQueueName { get; set; } = null!;

        [Description("Service bus namespace")]
        [CommandOption("-t|--transformation")]
        public string? Transformation { get; set; }
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        var queueName = settings.QueueName;

        await queueOperations.EnsureQueueExists(settings.QueueName, cancellationToken);
        await queueOperations.EnsureQueueExists(settings.TargetQueueName, cancellationToken);


        await AnsiConsole.Progress()
            .Columns(new TaskDescriptionColumn(), new SpinnerColumn())
            .HideCompleted(true)
            .StartAsync(async ctx =>
            {
                var queuesProgress = ctx.AddTask($"Fetching DLQ status for {queueName}", autoStart: true);

                var queue = await queueOperations.GetQueueRuntimeProperties(queueName, cancellationToken);
                queuesProgress.StopTask();

                if (queue.DeadLetterMessageCount == 0)
                {
                    AnsiConsole.WriteLine($"No DLQ messages to move found in {queueName}");
                    return;
                }

                var progress = ctx.AddTask($"Moving DLQ messages for {queueName} into {settings.TargetQueueName}", autoStart: true, queue.DeadLetterMessageCount);

                var transformationKey = settings.Transformation ?? "default";

                QueueOperations.Transformation transformation = transformationKey.ToLowerInvariant() switch
                {
                    "default" => new DefaultTransformation(),
                    "none" => new NoneTransformation(),
                    "nservicebus" => new NServiceBusTransformation(),
                    _ => throw new Exception($"Unknown transformation: {transformationKey}")
                };

                var dlqMessages = await queueOperations.MoveDeadLetterMessages(queueName, settings.TargetQueueName, transformation, progress, cancellationToken);

                progress.StopTask();

                DisplayHelper.ShowDLQMessages(queue.Name, SubQueue.DeadLetter, dlqMessages);
            });

        return 0;
    }

    class DefaultTransformation : QueueOperations.Transformation
    {
        public override void Transform(string sourceQueue, ServiceBusReceivedMessage dlqMessage, ServiceBusMessage message)
        {
            message.ApplicationProperties["x-asb-dlq-source-queue"] = sourceQueue;
            message.ApplicationProperties["x-asb-dlq-reason"] = dlqMessage.DeadLetterReason;
            message.ApplicationProperties["x-asb-dlq-description"] = dlqMessage.DeadLetterErrorDescription;
        }
    }

    class NoneTransformation : QueueOperations.Transformation
    {
        public override void Transform(string sourceQueue, ServiceBusReceivedMessage dlqMessage, ServiceBusMessage message)
        {
            //no-op
        }
    }

    class NServiceBusTransformation : QueueOperations.Transformation
    {
        public override void Transform(string sourceQueue, ServiceBusReceivedMessage dlqMessage, ServiceBusMessage message)
        {
            message.ApplicationProperties[NServiceBus.Headers.FailedQ] = sourceQueue;
            message.ApplicationProperties[NServiceBus.Headers.ExceptionType] = dlqMessage.DeadLetterReason;
            message.ApplicationProperties[NServiceBus.Headers.Message] = dlqMessage.DeadLetterErrorDescription;
        }
    }
}