using System.ComponentModel;
using Azure.Identity;
using Azure.Messaging.ServiceBus.Administration;
using Microsoft.Extensions.Azure;
using Spectre.Console;
using Spectre.Console.Cli;

public class StatusCommand : AsyncCommand<StatusCommand.Settings>
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

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var table = new Table
        {
            Title = new TableTitle("DLQ Status")
        };

        table.AddColumn("Queue");
        table.AddColumn(new TableColumn("Count").Centered());

        await foreach (var queue in settings.AdministrationClient.GetQueuesAsync())
        {
            table.AddRow(queue.Name, "1");
        }

        AnsiConsole.Write(table);

        return 0;
    }
}