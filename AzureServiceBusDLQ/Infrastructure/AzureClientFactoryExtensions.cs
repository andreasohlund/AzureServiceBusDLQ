using Azure.Identity;
using Azure.Messaging.ServiceBus.Administration;
using Microsoft.Extensions.DependencyInjection;

public static class AzureClientFactoryExtensions
{
    public static void AddAzureClients(this ServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton(sp =>
        {
            var settings = sp.GetRequiredService<CommandSettingsProvider>().Settings as BaseSettings;

            if (settings is null)
            {
                throw new InvalidOperationException("All commands must use BaseSettings");
            }

            return settings.Namespace is null ? new ServiceBusAdministrationClient(settings.ConnectionString) : new ServiceBusAdministrationClient(settings.Namespace, new DefaultAzureCredential());
        });
    }
}