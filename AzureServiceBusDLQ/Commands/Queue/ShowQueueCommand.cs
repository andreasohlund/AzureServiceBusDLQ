using System.ComponentModel;
using Azure.Messaging.ServiceBus.Administration;
using Spectre.Console;
using Spectre.Console.Cli;

public class ShowQueueCommand(ServiceBusAdministrationClient administrationClient) : CancellableAsyncCommand<ShowQueueCommand.Settings>
{
    public class Settings : BaseSettings
    {
        [Description("Queue name to be displayed")]
        [CommandArgument(0, "<QueueName>")]
        public string QueueName { get; set; } = null!;
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        var dlqMessagesExists = false;

        var existResult = await administrationClient.QueueExistsAsync(settings.QueueName, cancellationToken);

        if (!existResult.Value)
        {
            throw new InvalidOperationException($"Queue {settings.QueueName} does not exist");
        }

        await AnsiConsole.Progress()
            .Columns(new TaskDescriptionColumn(), new SpinnerColumn())
            .HideCompleted(true)
            .StartAsync(async ctx =>
            {
                var queuesProgress = ctx.AddTask($"Fetching DLQ status for {settings.QueueName}");

                var result = await administrationClient.GetQueueRuntimePropertiesAsync(settings.QueueName, cancellationToken);

                var properties = result.Value;

                queuesProgress.StopTask();

                var queueTable = new Table
                {
                    Title = new TableTitle($"DLQ Messages in {properties.Name}")
                };

                queueTable.AddColumn("Queue");
                queueTable.AddColumn(new TableColumn("DLQ").Centered());
                queueTable.AddColumn(new TableColumn("TDLQ").Centered());

//                foreach (var queue in queuesWithDLQMessages)
//                {
//                    queueTable.AddRow(queue.Name, queue.DeadLetterMessageCount.ToString(), queue.TransferDeadLetterMessageCount.ToString());
//                }

                AnsiConsole.Write(queueTable);
            });


        return dlqMessagesExists ? 1 : 0;
    }
}