using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Spectre.Console;

public class QueueOperations(ServiceBusAdministrationClient administrationClient, ServiceBusClient serviceBusClient)
{
    public async Task EnsureQueueExists(string queueName, CancellationToken cancellationToken)
    {
        var existResult = await administrationClient.QueueExistsAsync(queueName, cancellationToken);

        if (!existResult.Value)
        {
            throw new InvalidOperationException($"Queue {queueName} does not exist");
        }
    }

    public async Task<QueueRuntimeProperties> GetQueueRuntimeProperties(string queueName, CancellationToken cancellationToken)
    {
        var result = await administrationClient.GetQueueRuntimePropertiesAsync(queueName, cancellationToken);

        if (!result.HasValue)
        {
            throw new InvalidOperationException($"Queue {queueName} does not exist");
        }
        
        return result.Value;
    }

    public async Task<List<ServiceBusReceivedMessage>> GetMessagesForSubQueue(QueueRuntimeProperties queue, SubQueue subQueue, ProgressTask messagePeekProgress, CancellationToken cancellationToken)
    {
        await using var receiver = serviceBusClient.CreateReceiver(queue.Name, new ServiceBusReceiverOptions { SubQueue = subQueue });

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

        return dlqMessages;
    }

    public async Task<List<ServiceBusReceivedMessage>> RetryDeadLetterMessages(QueueRuntimeProperties queue, ProgressTask progress, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}