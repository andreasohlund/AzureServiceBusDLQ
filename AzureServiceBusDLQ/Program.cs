﻿using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

var serviceCollection = new ServiceCollection();
var contextProvider = new CommandSettingsProvider();

serviceCollection.AddSingleton(contextProvider);
serviceCollection.AddAzureClients();

var app = new CommandApp(new TypeRegistrar(serviceCollection));

app.Configure(config =>
{
    config.SetInterceptor(new CommandSettingsInterceptor(contextProvider));
    config.AddCommand<QueuesStatusCommand>("queues");
    config.AddCommand<SubscriptionsStatusCommand>("subscriptions");
});

await app.RunAsync(args);