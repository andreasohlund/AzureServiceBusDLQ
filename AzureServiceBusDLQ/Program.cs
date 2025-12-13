using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using Spectre.Console.Cli;

public static class Program
{
    public static Task<int> Main(string[] args)
        => Run(args, AnsiConsole.Console);

    public static async Task<int> Run(string[] args, IAnsiConsole console)
    {
        var serviceCollection = new ServiceCollection();
        var contextProvider = new CommandSettingsProvider();

        serviceCollection.AddSingleton(contextProvider);
        serviceCollection.AddAzureClients();

        var app = new CommandApp(new TypeRegistrar(serviceCollection));

        app.Configure(config =>
        {
            config.ConfigureConsole(console);
            config.SetInterceptor(new CommandSettingsInterceptor(contextProvider));
            config.AddCommand<QueuesStatusCommand>("queues");
            config.AddCommand<ShowQueueCommand>("queue");
            config.AddCommand<RetryQueueCommand>("retry-queue");
            config.AddCommand<MoveQueueCommand>("move-dlq-messages");
            config.AddCommand<SubscriptionsStatusCommand>("subscriptions");
#if DEBUG
            config.ValidateExamples();
#endif
        });

        return await app.RunAsync(args);
    }
}