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

        await foreach (var queue in settings.AdministrationClient.GetQueuesAsync(cancellationToken))
        {
            var response = await settings.AdministrationClient.GetQueueRuntimePropertiesAsync(queue.Name, cancellationToken);
            table.AddRow(queue.Name, response.Value.DeadLetterMessageCount.ToString());
        }

        AnsiConsole.Write(table);

        return 0;
    }
}