using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

var registrations = new ServiceCollection();
var registrar = new TypeRegistrar(registrations);
var app = new CommandApp(registrar);

app.Configure(config =>
{
    config.AddCommand<QueuesStatusCommand>("queues");
    config.AddCommand<SubscriptionsStatusCommand>("subscriptions");
});

await app.RunAsync(args);