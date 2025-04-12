using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

var serviceCollection = new ServiceCollection();
var contextProvider = new CommandSettingsProvider();

serviceCollection.AddSingleton(contextProvider);
serviceCollection.AddAzureClients();

var app = new CommandApp(new TypeRegistrar(serviceCollection));

app.Configure(config =>
{
#if DEBUG
    config.PropagateExceptions();
    config.ValidateExamples();
#endif
    config.SetInterceptor(new CommandSettingsInterceptor(contextProvider));
    config.AddCommand<QueuesStatusCommand>("queues");
    config.AddCommand<ShowQueueCommand>("queue");
    config.AddCommand<RetryQueueCommand>("retry-queue");
    config.AddCommand<SubscriptionsStatusCommand>("subscriptions");
});

return await app.RunAsync(args);