using System.ComponentModel;
using Azure.Identity;
using Azure.Messaging.ServiceBus.Administration;
using Spectre.Console;
using Spectre.Console.Cli;

public abstract class BaseSettings : CommandSettings
{
    [Description("Service bus namespace")]
    [CommandOption("-n|--namespace")]
    public string? Namespace { get; set; }

    public override ValidationResult Validate()
    {
        return Namespace == null ? ValidationResult.Error("Namespace must be specified") : ValidationResult.Success();
    }

    public ServiceBusAdministrationClient AdministrationClient => new(Namespace, new DefaultAzureCredential());
}