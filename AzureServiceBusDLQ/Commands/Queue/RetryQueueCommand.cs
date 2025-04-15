using System.ComponentModel;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Spectre.Console;
using Spectre.Console.Cli;

public class RetryQueueCommand(QueueOperations queueOperations) : CancellableAsyncCommand<RetryQueueCommand.Settings>
{
    public class Settings : BaseSettings
    {
        [Description("Queue name to be retried")]
        [CommandArgument(0, "<QueueName>")]
        public string QueueName { get; set; } = null!;
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        await queueOperations.EnsureQueueExists(settings.QueueName, cancellationToken);

        var queueName = settings.QueueName;

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
                    AnsiConsole.WriteLine($"No DLQ messages to retry found in {queueName}");
                    return;
                }

                var progress = ctx.AddTask($"Retrying DLQ messages for {queueName}", autoStart: true, queue.DeadLetterMessageCount);

                var dlqMessages = await queueOperations.RetryDeadLetterMessages(queue, progress, cancellationToken);

                progress.StopTask();

                DisplayHelper.ShowDLQMessages(queue.Name, SubQueue.DeadLetter, dlqMessages);
            });
        
        return 0;
    }
}