using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

public class BaseSettings : CommandSettings
{
    [Description("Service bus namespace")]
    [CommandOption("-n|--namespace")]
    public string? Namespace { get; set; }

    [Description("Service bus connection string")]
    [CommandOption("-c|--connection-string")]
    public string? ConnectionString { get; set; }

    public override ValidationResult Validate()
    {
        if (Namespace is null && ConnectionString is null)
        {
            return ValidationResult.Error("Namespace or connection string must be specified");
        }

        return ValidationResult.Success();
    }
}