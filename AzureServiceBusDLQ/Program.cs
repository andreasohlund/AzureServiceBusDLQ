using Azure.Messaging.ServiceBus.Administration;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

var connectionString = Environment.GetEnvironmentVariable("AzureServiceBus_ConnectionString");
var adminClient = new ServiceBusAdministrationClient(connectionString);

var serviceCollection = new ServiceCollection();

serviceCollection.AddSingleton(adminClient);

// Create a type registrar and register any dependencies.
// A type registrar is an adapter for a DI framework.
var registrar = new TypeRegistrar(serviceCollection);

var app = new CommandApp(registrar);
app.Configure(config =>
{
    config.AddCommand<StatusCommand>("status");
});

await app.RunAsync(args);