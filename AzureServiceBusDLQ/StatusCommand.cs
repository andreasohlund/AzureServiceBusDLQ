using Azure.Messaging.ServiceBus.Administration;
using Spectre.Console;
using Spectre.Console.Cli;

public class StatusCommand(ServiceBusAdministrationClient serviceBusAdministrationClient):AsyncCommand<StatusCommand.Settings>
{
    public class Settings : CommandSettings
    {
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var table = new Table
        {
            Title = new TableTitle("DLQ Status")
        };

        table.AddColumn("Queue");
        table.AddColumn(new TableColumn("Count").Centered());

        await foreach (var queue in serviceBusAdministrationClient.GetQueuesAsync())
        {
            table.AddRow(queue.Name, "1");
        }

        AnsiConsole.Write(table);

        return 0;
    }
}