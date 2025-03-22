using System.Diagnostics;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using NUnit.Framework;

public class CommandTestFixture
{
    protected static string ConnectionString = Environment.GetEnvironmentVariable("AzureServiceBusDLQ_ConnectionString")!;

    protected string ServiceBusNamespace
    {
        get
        {
            return ConnectionString.Split(";")
                .Single(p=>p.StartsWith("Endpoint="))
                .Split("=")[1]
                .Replace("sb://", "")
                .Replace("/", "");
        }
    }

    [SetUp]
    public void Setup()
        => client = new ServiceBusAdministrationClient(ConnectionString);

    protected static async Task<string> ExecuteAndExpectSuccess(string command, string? connectionOptions = null)
    {
        connectionOptions ??= "-c " + ConnectionString;

        var process = new Process();
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.WorkingDirectory = TestContext.CurrentContext.TestDirectory;
        process.StartInfo.FileName = "dotnet";
        process.StartInfo.Arguments = $"AzureServiceBusDLQ.dll {command} {connectionOptions}";

        process.Start();
        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();
        process.WaitForExit(10000); //TODO: async

        var output = await outputTask;
        var error = await errorTask;

        if (output != string.Empty)
        {
            Console.WriteLine(output);
        }

        Assert.Multiple(() =>
        {
            Assert.That(process.ExitCode, Is.EqualTo(0));
            Assert.That(error, Is.EqualTo(string.Empty));
        });

        return output;
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
}