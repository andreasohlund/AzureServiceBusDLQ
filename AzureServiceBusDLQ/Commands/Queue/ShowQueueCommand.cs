using System.ComponentModel;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Spectre.Console;
using Spectre.Console.Cli;

public class ShowQueueCommand(ServiceBusAdministrationClient administrationClient, ServiceBusClient serviceBusClient) : CancellableAsyncCommand<ShowQueueCommand.Settings>
{
    public class Settings : BaseSettings
    {
        [Description("Queue name to be displayed")]
        [CommandArgument(0, "<QueueName>")]
        public string QueueName { get; set; } = null!;
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        var dlqMessagesExists = false;

        var existResult = await administrationClient.QueueExistsAsync(settings.QueueName, cancellationToken);

        if (!existResult.Value)
        {
            throw new InvalidOperationException($"Queue {settings.QueueName} does not exist");
        }

        await AnsiConsole.Progress()
            .Columns(new TaskDescriptionColumn(), new SpinnerColumn())
            .HideCompleted(true)
            .StartAsync(async ctx =>
            {
                var queuesProgress = ctx.AddTask($"Fetching DLQ status for {settings.QueueName}", autoStart: true);

                var result = await administrationClient.GetQueueRuntimePropertiesAsync(settings.QueueName, cancellationToken);
                queuesProgress.StopTask();

                var queue = result.Value;

                dlqMessagesExists = queue.DeadLetterMessageCount > 0 || queue.TransferDeadLetterMessageCount > 0;
                if (!dlqMessagesExists)
                {
                    AnsiConsole.WriteLine($"No DLQ messages found in {queue.Name}");
                    return;
                }

                var messagePeekProgress = ctx.AddTask($"Fetching DLQ status for {settings.QueueName}", autoStart: true, queue.DeadLetterMessageCount);

                await using var receiver = serviceBusClient.CreateReceiver(queue.Name, new ServiceBusReceiverOptions { SubQueue = SubQueue.DeadLetter });

                var dlqMessages = new List<ServiceBusReceivedMessage>();
                var previousSequenceNumber = -1L;
                var sequenceNumber = 0L;

                do
                {
                    var messageBatch = await receiver.PeekMessagesAsync(int.MaxValue, sequenceNumber, cancellationToken);

                    if (messageBatch.Count > 0)
                    {
                        sequenceNumber = messageBatch[^1].SequenceNumber;

                        if (sequenceNumber == previousSequenceNumber)
                            break;

                        dlqMessages.AddRange(messageBatch);

                        messagePeekProgress.Increment(messageBatch.Count);
                        previousSequenceNumber = sequenceNumber;
                    }
                    else
                    {
                        break;
                    }
                } while (true);

                messagePeekProgress.StopTask();

                var queueTable = new Table
                {
                    Title = new TableTitle($"DLQ Messages in {queue.Name} ({dlqMessages.Count})")
                };

                queueTable.AddColumn("MessageId");
                queueTable.AddColumn(new TableColumn("DLQ Reason").Centered());
                queueTable.AddColumn(new TableColumn("DLQ Description").Centered());
                queueTable.AddColumn(new TableColumn("DLQ Source").Centered());

                foreach (var message in dlqMessages)
                {
                    queueTable.AddRow(message.MessageId, message.DeadLetterReason ?? "", message.DeadLetterErrorDescription ?? "", message.DeadLetterSource ?? "");
                }

                AnsiConsole.Write(queueTable);
            });


        return dlqMessagesExists ? 1 : 0;
    }
}