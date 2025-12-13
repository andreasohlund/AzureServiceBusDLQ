using System.Diagnostics;
using System.Transactions;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using NUnit.Framework;
using Spectre.Console;

public class CommandTestFixture
{
    protected static readonly string ConnectionString = Environment.GetEnvironmentVariable("AzureServiceBusDLQ_ConnectionString")!;
    ServiceBusAdministrationClient AdministrationClient = null!;
    protected ServiceBusClient ServiceBusClient = null!;
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

    protected Task<CommandResult> ExecuteCommand(params string[] args)
    {
        var arguments = new List<string>(args)
        {
            "-c",
            ConnectionString
        };

        return ExecuteCommandWithoutDefaultConnection(arguments.ToArray());
    }

    protected async Task<CommandResult> ExecuteCommandWithoutDefaultConnection(params string[] args)
    {
        var output = new StringWriter();

        var console = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Out = new AnsiConsoleOutput(output),
            Ansi = AnsiSupport.No
        });
        
        var exitCode = await Program.Run(args, console);
        return new CommandResult(exitCode, output.ToString());
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

    CancellationTokenSource testCancellationTokenSource = null!;

    const string TestQueueNamePrefix = "asq-dlq-test";
    static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(10);
}