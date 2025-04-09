using Azure.Messaging.ServiceBus.Administration;
using Spectre.Console;
using Spectre.Console.Cli;

public class QueuesStatusCommand(ServiceBusAdministrationClient administrationClient) : CancellableAsyncCommand<BaseSettings>
{
    protected override async Task<int> ExecuteAsync(CommandContext context, BaseSettings settings, CancellationToken cancellationToken)
    {
        var dlqMessagesExists = false;

        await AnsiConsole.Progress()
            .Columns(new TaskDescriptionColumn(), new SpinnerColumn())
            .HideCompleted(true)
            .StartAsync(async ctx =>
            {
                var queuesProgress = ctx.AddTask($"Fetching queue DLQ status");

                var queues = await GetQueueRuntimeProperties(administrationClient, queuesProgress, cancellationToken);

                var queuesWithDLQMessages = queues.Where(q => q.DeadLetterMessageCount > 0 || q.TransferDeadLetterMessageCount > 0)
                    .ToList();

                dlqMessagesExists = queuesWithDLQMessages.Any();
                if (!dlqMessagesExists)
                {
                    AnsiConsole.WriteLine($"No DLQ messages found in {queues.Count} queues");
                    return;
                }

                var queueTable = new Table
                {
                    Title = new TableTitle($"DLQ Status ({queues.Count} queues checked)")
                };

                queueTable.AddColumn("Queue");
                queueTable.AddColumn(new TableColumn("DLQ").Centered());
                queueTable.AddColumn(new TableColumn("TDLQ").Centered());

                foreach (var queue in queuesWithDLQMessages)
                {
                    queueTable.AddRow(queue.Name, queue.DeadLetterMessageCount.ToString(), queue.TransferDeadLetterMessageCount.ToString());
                }

                AnsiConsole.Write(queueTable);
            });


        return dlqMessagesExists ? 1 : 0;
    }

    async Task<List<QueueRuntimeProperties>> GetQueueRuntimeProperties(ServiceBusAdministrationClient client, ProgressTask progressTask, CancellationToken cancellationToken)
    {
        var queues = new List<QueueRuntimeProperties>();

        await foreach (var queue in client.GetQueuesRuntimePropertiesAsync(cancellationToken))
        {
            queues.Add(queue);
            progressTask.Increment(1);
        }

        progressTask.StopTask();

        return queues;
    }
}