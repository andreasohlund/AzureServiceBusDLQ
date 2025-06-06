﻿using System.Diagnostics;
using System.Transactions;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using NUnit.Framework;

public class CommandTestFixture
{
    protected static readonly string ConnectionString = Environment.GetEnvironmentVariable("AzureServiceBusDLQ_ConnectionString")!;
    protected ServiceBusAdministrationClient AdministrationClient;
    protected ServiceBusClient ServiceBusClient;
    protected CancellationToken TestTimeoutCancellationToken => testCancellationTokenSource.Token;
    protected string TestQueueName => $"{TestQueueNamePrefix}-{TestContext.CurrentContext.Test.ID}";

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
    public async Task Setup()
    {
        AdministrationClient = new ServiceBusAdministrationClient(ConnectionString);
        ServiceBusClient = new ServiceBusClient(ConnectionString);

        testCancellationTokenSource = Debugger.IsAttached ? new CancellationTokenSource() : new CancellationTokenSource(TestTimeout);

        await ClearAllTestQueues();
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
            TestContext.WriteLine(output);
        }

        if (error != string.Empty)
        {
            TestContext.WriteLine(error);
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

    protected async Task CreateQueue(string queueName)
    {
        await DeleteQueue(queueName);
        await AdministrationClient.CreateQueueAsync(queueName, TestTimeoutCancellationToken);
    }

    protected async Task CreateQueueWithDLQMessage(string queueName, ServiceBusMessage? message = null)
    {
        await CreateQueue(queueName);

        await AddDLQMessage(queueName, message);
    }
    
    protected async Task AddDLQMessage(string queueName, ServiceBusMessage? message = null)
    {
        await using var sender = ServiceBusClient.CreateSender(queueName);
        
        message ??= new ServiceBusMessage(BinaryData.FromObjectAsJson(new { MyProperty = "Some value" }));

        await sender.SendMessageAsync(message, TestTimeoutCancellationToken);

        await using var receiver = ServiceBusClient.CreateReceiver(queueName);

        var receivedMessage = await receiver.ReceiveMessageAsync(cancellationToken: TestTimeoutCancellationToken);

        await receiver.DeadLetterMessageAsync(receivedMessage, "Some reason", "Some description", TestTimeoutCancellationToken);
    }

    protected async Task CreateQueueWithTDLQMessage(string queueName)
    {
        var targetQueueName = queueName + "-via";
        await CreateQueue(queueName);
        await CreateQueue(targetQueueName);

        await using var seedMessageSender = ServiceBusClient.CreateSender(queueName);

        await seedMessageSender.SendMessageAsync(new ServiceBusMessage(), TestTimeoutCancellationToken);

        var transactionalClient = new ServiceBusClient(ConnectionString, new ServiceBusClientOptions
        {
            EnableCrossEntityTransactions = true
        });
        
        await using var receiver = transactionalClient.CreateReceiver(queueName);
        await using var sendViaSender = transactionalClient.CreateSender(targetQueueName);

        var message = await receiver.ReceiveMessageAsync(cancellationToken: TestTimeoutCancellationToken);

        using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
        
        await sendViaSender.SendMessageAsync(new ServiceBusMessage(), TestTimeoutCancellationToken);

        QueueProperties targetQueueProperties = await AdministrationClient.GetQueueAsync(targetQueueName, TestTimeoutCancellationToken);
        targetQueueProperties.Status = EntityStatus.Disabled;
        await AdministrationClient.UpdateQueueAsync(targetQueueProperties, TestTimeoutCancellationToken);

        await receiver.CompleteMessageAsync(message, TestTimeoutCancellationToken);
       
        scope.Complete();
    }

    async Task DeleteQueue(string queueName)
    {
        try
        {
            await AdministrationClient.DeleteQueueAsync(queueName, TestTimeoutCancellationToken);
        }
        catch (ServiceBusException ex) when (ex.Reason == ServiceBusFailureReason.MessagingEntityNotFound)
        {
        }
    }
    
    protected async Task ClearAllTestQueues()
    {
        await foreach (var queue in AdministrationClient.GetQueuesRuntimePropertiesAsync(TestTimeoutCancellationToken))
        {
            if (queue.Name.StartsWith(TestQueueNamePrefix))
            {
                await AdministrationClient.DeleteQueueAsync(queue.Name, TestTimeoutCancellationToken);
            }
        }
    }
    
    CancellationTokenSource testCancellationTokenSource;

    const string TestQueueNamePrefix = "asq-dlq-test";
    static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(10);
}