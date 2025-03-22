using NUnit.Framework;

namespace AzureServiceBusDLQ.Tests;

[TestFixture]
public class ConnectionTests : CommandTestFixture
{
    [Test]
    public async Task SupportConnectionString()
    {
        _ = await ExecuteAndExpectSuccess($"queues", "-c " + ConnectionString);
    }
    
    [Test]
    public async Task SupportNamespaceWithDefaultCredentials()
    {
        _ = await ExecuteAndExpectSuccess($"queues", "-n " + ServiceBusNamespace);
    }
}