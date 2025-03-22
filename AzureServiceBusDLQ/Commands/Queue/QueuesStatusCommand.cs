using Azure.Messaging.ServiceBus.Administration;
using Spectre.Console;
using Spectre.Console.Cli;

public class QueuesStatusCommand(ServiceBusAdministrationClient administrationClient) : CancellableAsyncCommand<BaseSettings>
{
    protected override async Task<int> ExecuteAsync(CommandContext context, BaseSettings settings, CancellationToken cancellationToken)
    {
        await AnsiConsole.Progress()
            .Columns(new TaskDescriptionColumn(), new SpinnerColumn())
            .HideCompleted(true)
            .StartAsync(async ctx =>
            {
                var queuesProgress = ctx.AddTask($"Fetching queue DLQ status");

                var queues = await GetQueueRuntimeProperties(administrationClient, queuesProgress, cancellationToken);

                var queueTable = new Table
                {
                    Title = new TableTitle("Queue DLQ Status")
                };

                queueTable.AddColumn("Queue");
                queueTable.AddColumn(new TableColumn("DLQ").Centered());
                queueTable.AddColumn(new TableColumn("TDLQ").Centered());

                foreach (var queue in queues)
                {
                    queueTable.AddRow(queue.Name, queue.DeadLetterMessageCount.ToString(), queue.TransferDeadLetterMessageCount.ToString());}

                AnsiConsole.Write(queueTable);
            });


        return 0;
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