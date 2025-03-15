using Azure.Messaging.ServiceBus.Administration;
using Spectre.Console;

//TODO: Use cmd line arg
var connectionString = Environment.GetEnvironmentVariable("AzureServiceBus_ConnectionString");

var adminClient = new ServiceBusAdministrationClient(connectionString);

var table = new Table
{
    Title = new TableTitle("DLQ Status")
};

table.AddColumn("Queue");
table.AddColumn(new TableColumn("Count").Centered());

await foreach (var queue in adminClient.GetQueuesAsync())
{
    table.AddRow(queue.Name, "1");
}

AnsiConsole.Write(table);