using NUnit.Framework;

[TestFixture]
public class QueuesStatusTests : CommandTestFixture
{
    [Test]
    public async Task ReturnsZeroWhenNoQueuesHaveDLQMessages()
    {
        _ = await ExecuteCommandAndExpectSuccess($"queues");
    }
    
    [Test]
    public async Task ReturnsNonZeroWhenAtLeastOneQueueHaveDLQMessages()
    {
        var result = await ExecuteCommand($"queues");
        
        Assert.That(result.ExitCode, Is.Not.Zero);
    }
}