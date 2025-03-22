using System.Diagnostics;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using NUnit.Framework;

[TestFixture]
public class CommandTestFixture
{
    [Test]
    public async Task Status_all_queues()
    {
        var (output, error, exitCode) = await Execute($"queues");

        Assert.Multiple(() =>
        {
            Assert.That(exitCode, Is.EqualTo(0));
            Assert.That(error, Is.EqualTo(string.Empty));
        });
    }

    [SetUp]
    public void Setup()
        => client = new ServiceBusAdministrationClient(Environment.GetEnvironmentVariable("AzureServiceBusDLQ_ConnectionString"));

    static async Task<(string output, string error, int exitCode)> Execute(string command)
    {
        var process = new Process();
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.WorkingDirectory = TestContext.CurrentContext.TestDirectory;
        process.StartInfo.FileName = "dotnet";
        process.StartInfo.Arguments = $"AzureServiceBusDLQ.dll " + command + " -n winservice-repro.servicebus.windows.net";

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

        if (error != string.Empty)
        {
            Console.WriteLine(error);
        }

        return (output, error, process.ExitCode);
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