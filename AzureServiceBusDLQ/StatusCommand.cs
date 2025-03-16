using System.ComponentModel;
using Azure.Identity;
using Azure.Messaging.ServiceBus.Administration;
using Spectre.Console;
using Spectre.Console.Cli;

public class StatusCommand : CancellableAsyncCommand<StatusCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [Description("Service bus namespace")]
        [CommandOption("-n|--namespace")]
        public string? Namespace { get; set; }

        public override ValidationResult Validate()
        {
            return Namespace == null ? ValidationResult.Error("Namespace must be specified") : ValidationResult.Success();
        }

        public ServiceBusAdministrationClient AdministrationClient => new(Namespace, new DefaultAzureCredential());
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        var table = new Table
        {
            Title = new TableTitle("DLQ Status")
        };

        table.AddColumn("Queue");
        table.AddColumn(new TableColumn("Count").Centered());

        var queues = new List<QueueProperties>();

        //TODO: Spinner
        await foreach (var queue in settings.AdministrationClient.GetQueuesAsync(cancellationToken))
        {
            queues.Add(queue);
        }

        await AnsiConsole.Progress()
            .Columns(new TaskDescriptionColumn(), new ProgressBarColumn(), new PercentageColumn(), new RemainingTimeColumn(), new SpinnerColumn())
            .StartAsync(async ctx =>
            {
                var task = ctx.AddTask($"Fetching DLQ status for {queues.Count} queues", new ProgressTaskSettings());

                task.MaxValue = queues.Count;
                await Task.WhenAll(queues.Select(async queue =>
                {
                    var response = await settings.AdministrationClient.GetQueueRuntimePropertiesAsync(queue.Name, cancellationToken);
                    if (!response.HasValue)
                    {
                        throw new InvalidOperationException($"Failed to get queue runtime properties for {queue.Name}");
                    }

                    task.Increment(1);
                    table.AddRow(queue.Name, response.Value.DeadLetterMessageCount.ToString());
                }));
            });

        AnsiConsole.Write(table);

        return 0;
    }
}