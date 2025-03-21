using Spectre.Console.Cli;

public class CommandSettingsInterceptor(CommandSettingsProvider commandSettingsProvider) : ICommandInterceptor
{
    public void Intercept(CommandContext context, CommandSettings settings)
    {
        commandSettingsProvider.Settings = settings;
    }
}