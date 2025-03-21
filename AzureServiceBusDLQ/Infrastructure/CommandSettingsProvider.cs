using Spectre.Console.Cli;

public record CommandSettingsProvider
{
    public CommandSettings? Settings { get; set; }
}