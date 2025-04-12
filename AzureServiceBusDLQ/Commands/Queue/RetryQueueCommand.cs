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
        var dlqMessagesExists = false;
        var queueName = settings.QueueName;

        await queueOperations.EnsureQueueExists(settings.QueueName, cancellationToken);

        await AnsiConsole.Progress()
            .Columns(new TaskDescriptionColumn(), new SpinnerColumn())
            .HideCompleted(true)
            .StartAsync(async ctx =>
            {
                var queuesProgress = ctx.AddTask($"Fetching DLQ status for {queueName}", autoStart: true);

                var queue = await queueOperations.GetQueueRuntimeProperties(queueName, cancellationToken);
                queuesProgress.StopTask();

                dlqMessagesExists = queue.DeadLetterMessageCount > 0;
                if (!dlqMessagesExists)
                {
                    AnsiConsole.WriteLine($"No DLQ messages to retry found in {queueName}");
                    return;
                }

                var progress = ctx.AddTask($"Retrying DLQ messages for {queueName}", autoStart: true, queue.DeadLetterMessageCount);

                var dlqMessages = await queueOperations.RetryDeadLetterMessages(queue,progress, cancellationToken);

                progress.StopTask();

                ShowDLQMessages(queue, dlqMessages);
            });

        return dlqMessagesExists ? 1 : 0;
    }

    static void ShowDLQMessages(QueueRuntimeProperties queue, List<ServiceBusReceivedMessage> dlqMessages)
    {
        var queueTable = new Table
        {
            Title = new TableTitle($"DLQ Messages in {queue.Name} ({dlqMessages.Count})")
        };

        queueTable.AddColumn("MessageId");
        queueTable.AddColumn(new TableColumn("DLQ Reason").Centered());
        queueTable.AddColumn(new TableColumn("DLQ Description").Centered());
        queueTable.AddColumn(new TableColumn("DLQ Source").Centered());

        foreach (var message in dlqMessages)
        {
            queueTable.AddRow(message.MessageId, message.DeadLetterReason ?? "", message.DeadLetterErrorDescription ?? "", message.DeadLetterSource ?? "");
        }

        AnsiConsole.Write(queueTable);
    }
}