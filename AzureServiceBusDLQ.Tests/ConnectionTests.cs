using NUnit.Framework;

[TestFixture]
public class ConnectionTests : CommandTestFixture
{
    [Test]
    public async Task SupportConnectionString()
    {
        var result = await ExecuteCommand($"queues", "-c " + ConnectionString);

        Assert.That(result.Error, Is.Empty);
    }

    [Test]
    public async Task SupportNamespaceWithDefaultCredentials()
    {
        var result = await ExecuteCommand($"queues", "-n " + ServiceBusNamespace);
        
        Assert.That(result.Error, Is.Empty);
    }
}