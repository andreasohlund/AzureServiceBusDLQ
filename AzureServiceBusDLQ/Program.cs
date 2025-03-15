using Spectre.Console.Cli;

var app = new CommandApp();

app.Configure(config =>
{
    config.AddCommand<StatusCommand>("status");
});

await app.RunAsync(args);