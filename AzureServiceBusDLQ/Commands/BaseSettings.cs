using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

public class BaseSettings : CommandSettings
{
    [Description("Service bus namespace")]
    [CommandOption("-n|--namespace")]
    public string? Namespace { get; set; }

    public override ValidationResult Validate()
    {
        return Namespace == null ? ValidationResult.Error("Namespace must be specified") : ValidationResult.Success();
    }
}