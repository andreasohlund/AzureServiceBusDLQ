using System.ComponentModel;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Spectre.Console;
using Spectre.Console.Cli;

public class ShowQueueCommand(QueueOperations queueOperations) : CancellableAsyncCommand<ShowQueueCommand.Settings>
{
    public class Settings : BaseSettings
    {
        [Description("Queue name to be displayed")]
        [CommandArgument(0, "<QueueName>")]
        public string QueueName { get; set; } = null!;
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        await queueOperations.EnsureQueueExists(settings.QueueName, cancellationToken);

        var dlqMessagesExists = false;

        await AnsiConsole.Progress()
            .Columns(new TaskDescriptionColumn(), new SpinnerColumn())
            .HideCompleted(true)
            .StartAsync(async ctx =>
            {
                var queuesProgress = ctx.AddTask($"Fetching DLQ status for {settings.QueueName}", autoStart: true);

                var queue = await queueOperations.GetQueueRuntimeProperties(settings.QueueName, cancellationToken);
                queuesProgress.StopTask();

                dlqMessagesExists = queue.DeadLetterMessageCount > 0 || queue.TransferDeadLetterMessageCount > 0;
                if (!dlqMessagesExists)
                {
                    AnsiConsole.WriteLine($"No DLQ messages found in {queue.Name}");
                    return;
                }

                if (queue.DeadLetterMessageCount > 0)
                {
                    var dlqProgress = ctx.AddTask($"Fetching DLQ status for {settings.QueueName}", autoStart: true, queue.DeadLetterMessageCount);

                    var dlqMessages = await queueOperations.GetMessagesForSubQueue(queue, SubQueue.DeadLetter, dlqProgress, cancellationToken);

                    dlqProgress.StopTask();

                    ShowDLQMessages(queue, dlqMessages);
                }

                if (queue.TransferDeadLetterMessageCount > 0)
                {
                    var tDlqProgress = ctx.AddTask($"Fetching TDLQ status for {settings.QueueName}", autoStart: true, queue.TransferDeadLetterMessageCount);

                    var tDlqMessages = await queueOperations.GetMessagesForSubQueue(queue, SubQueue.TransferDeadLetter, tDlqProgress, cancellationToken);

                    tDlqProgress.StopTask();

                    ShowDLQMessages(queue, tDlqMessages);
                }
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