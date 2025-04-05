using System.Diagnostics;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using NUnit.Framework;

public class CommandTestFixture
{
    protected static readonly string ConnectionString = Environment.GetEnvironmentVariable("AzureServiceBusDLQ_ConnectionString")!;

    CancellationToken TestTimeoutCancellationToken => testCancellationTokenSource.Token;

    protected static string ServiceBusNamespace
    {
        get
        {
            return ConnectionString.Split(";")
                .Single(p => p.StartsWith("Endpoint="))
                .Split("=")[1]
                .Replace("sb://", "")
                .Replace("/", "");
        }
    }

    [SetUp]
    public void Setup()
    {
        client = new ServiceBusAdministrationClient(ConnectionString);

        testCancellationTokenSource = Debugger.IsAttached ? new CancellationTokenSource() : new CancellationTokenSource(TestTimeout);
    }

    [TearDown]
    public void Cleanup()
    {
        testCancellationTokenSource?.Dispose();
    }

    protected async Task<CommandResult> ExecuteCommand(string command, string? connectionOptions = null)
    {
        connectionOptions ??= "-c " + ConnectionString;

        var process = new Process();
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.WorkingDirectory = TestContext.CurrentContext.TestDirectory;
        process.StartInfo.FileName = "dotnet";
        process.StartInfo.Arguments = $"AzureServiceBusDLQ.dll {command} {connectionOptions}";

        process.Start();
        var outputTask = process.StandardOutput.ReadToEndAsync(TestTimeoutCancellationToken);
        var errorTask = process.StandardError.ReadToEndAsync(TestTimeoutCancellationToken);

        await process.WaitForExitAsync(TestTimeoutCancellationToken);

        var output = await outputTask.WaitAsync(TestTimeoutCancellationToken);
        var error = await errorTask.WaitAsync(TestTimeoutCancellationToken);

        if (output != string.Empty)
        {
            Console.WriteLine(output);
        }

        if (error != string.Empty)
        {
            Console.WriteLine(error);
        }

        return new CommandResult(process.ExitCode, output, error);
    }

    protected async Task<string> ExecuteCommandAndExpectSuccess(string command, string? connectionOptions = null)
    {
        var result = await ExecuteCommand(command, connectionOptions);

        Assert.Multiple(() =>
        {
            Assert.That(result.ExitCode, Is.EqualTo(0));
            Assert.That(result.Error, Is.EqualTo(string.Empty));
        });

        return result.Output;
    }

    async Task DeleteQueue(string queueName)
    {
        try
        {
            await client.DeleteQueueAsync(queueName);
        }
        catch (ServiceBusException ex) when (ex.Reason == ServiceBusFailureReason.MessagingEntityNotFound)
        {
        }
    }

    async Task DeleteTopic(string topicName)
    {
        try
        {
            await client.DeleteTopicAsync(topicName);
        }
        catch (ServiceBusException ex) when (ex.Reason == ServiceBusFailureReason.MessagingEntityNotFound)
        {
        }
    }

    ServiceBusAdministrationClient client;
    CancellationTokenSource testCancellationTokenSource;

    static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(30);
}