using System.ComponentModel;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Spectre.Console;
using Spectre.Console.Cli;

public class MoveQueueCommand(QueueOperations queueOperations) : CancellableAsyncCommand<MoveQueueCommand.Settings>
{
    public class Settings : BaseSettings
    {
        [Description("Queue name which messages will be moved")]
        [CommandArgument(0, "<QueueName>")]
        public string QueueName { get; set; } = null!;
        [Description("Queue name which messages will be moved")]
        [CommandArgument(0, "<TargetQueueName>")]
        public string TargetQueueName { get; set; } = null!;
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

                var dlqMessages = await queueOperations.MoveDeadLetterMessages(queueName, settings.TargetQueueName, progress, cancellationToken);

                progress.StopTask();

                DisplayHelper.ShowDLQMessages(queue.Name, SubQueue.DeadLetter, dlqMessages);
            });
        
        return 0;
    }
}