using NUnit.Framework;

[TestFixture]
public class ConnectionTests : CommandTestFixture
{
    [Test]
    public async Task SupportConnectionString()
    {
        var result = await ExecuteCommandWithoutDefaultConnection("queues", "-c ", ConnectionString);

        Assert.That(result.ExitCode, Is.Zero);
    }

    [Test]
    public async Task SupportNamespaceWithDefaultCredentials()
    {
        var result = await ExecuteCommandWithoutDefaultConnection("queues", "-n ", ServiceBusNamespace);

        Assert.That(result.ExitCode, Is.Zero);
    }
}