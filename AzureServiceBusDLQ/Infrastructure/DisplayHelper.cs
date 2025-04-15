using Azure.Messaging.ServiceBus;
using Spectre.Console;

static class DisplayHelper
{
    public static void ShowDLQMessages(string queueName, SubQueue subQueue, List<ServiceBusReceivedMessage> messages)
    {
        if (subQueue == SubQueue.None)
        {
            throw new InvalidOperationException("SubQueue None is not supported");
        }

        var dlqName = subQueue == SubQueue.DeadLetter ? "DLQ" : "TDLQ";

        var queueTable = new Table
        {
            Title = new TableTitle($"{dlqName} Messages in {queueName} ({messages.Count})")
        };

        queueTable.AddColumn("MessageId");
        queueTable.AddColumn(new TableColumn($"{dlqName} Reason").Centered());
        queueTable.AddColumn(new TableColumn($"{dlqName} Description").Centered());
        queueTable.AddColumn(new TableColumn($"{dlqName} Source").Centered());

        foreach (var message in messages)
        {
            queueTable.AddRow(message.MessageId, message.DeadLetterReason ?? "", message.DeadLetterErrorDescription ?? "", message.DeadLetterSource ?? "");
        }

        AnsiConsole.Write(queueTable);
    }
}