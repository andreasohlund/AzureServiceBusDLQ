using NUnit.Framework;

[TestFixture]
public class ConnectionTests : CommandTestFixture
{
    [Test]
    public async Task SupportConnectionString()
    {
        _ = await ExecuteCommandAndExpectSuccess($"queues", "-c " + ConnectionString);
    }
    
    [Test]
    public async Task SupportNamespaceWithDefaultCredentials()
    {
        _ = await ExecuteCommandAndExpectSuccess($"queues", "-n " + ServiceBusNamespace);
    }
}