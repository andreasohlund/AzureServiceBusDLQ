//using Azure.Messaging.ServiceBus;
//
//class TransferDeadLetterMessages
//{
//    private static ServiceBusClient client;
//    private static ServiceBusSender sender;
//    
//    public static async Task ProcessTopicAsync(string connectionString, string topicName, string subscriberName, int fetchCount = 10)
//    {
//        try
//        {
//            client = new ServiceBusClient(connectionString);
//            sender = client.CreateSender(topicName);
//
//            ServiceBusReceiver dlqReceiver = client.CreateReceiver(topicName, subscriberName, new ServiceBusReceiverOptions
//            {
//                SubQueue = SubQueue.DeadLetter,
//                ReceiveMode = ServiceBusReceiveMode.PeekLock
//            });
//
//            await ProcessDeadLetterMessagesAsync($"topic: {topicName} -> subscriber: {subscriberName}", fetchCount, sender, dlqReceiver);
//        }
//        catch (ServiceBusException ex)
//        {
//            if (ex.Reason == Azure.Messaging.ServiceBus.ServiceBusFailureReason.MessagingEntityNotFound)
//            {
//                throw new Exception($"Topic:Subscriber '{topicName}:{subscriberName}' not found. Check that the name provided is correct.");
//            }
//            else
//            {
//                throw;
//            }
//        }
//        finally
//        {
//            await sender.CloseAsync();
//            await client.DisposeAsync();
//        }
//    }
//
//    public static async Task ProcessQueueAsync(string connectionString, string queueName, int fetchCount = 10)
//    {         
//        try
//        {
//            client = new ServiceBusClient(connectionString);
//            sender = client.CreateSender(queueName);
//
//            ServiceBusReceiver dlqReceiver = client.CreateReceiver(queueName, new ServiceBusReceiverOptions
//            {
//                SubQueue = SubQueue.DeadLetter,
//                ReceiveMode = ServiceBusReceiveMode.PeekLock
//            });
//
//            await ProcessDeadLetterMessagesAsync($"queue: {queueName}", fetchCount, sender, dlqReceiver);
//        }
//        catch (Azure.Messaging.ServiceBus.ServiceBusException ex)
//        {
//            if (ex.Reason == Azure.Messaging.ServiceBus.ServiceBusFailureReason.MessagingEntityNotFound)
//            {
//                throw new.Error(ex, $"Queue '{queueName}' not found. Check that the name provided is correct.");
//            }
//            else
//            {
//                throw;
//            }
//        }
//        finally
//        {
//            await sender.CloseAsync();
//            await client.DisposeAsync();
//        }
//    }
//
//    private static async Task ProcessDeadLetterMessagesAsync(string source, int fetchCount, ServiceBusSender sender, ServiceBusReceiver dlqReceiver)
//    {
//        var wait = new System.TimeSpan(0, 0, 10);
//
//        Console.WriteLine($"fetching messages ({wait.TotalSeconds} seconds retrieval timeout)");
//        Console.WriteLine(source);
//
//        IReadOnlyList<ServiceBusReceivedMessage> dlqMessages = await dlqReceiver.ReceiveMessagesAsync(fetchCount, wait);
//
//        Console.WriteLine($"dl-count: {dlqMessages.Count}");
//
//        int i = 1;
//
//        foreach (var dlqMessage in dlqMessages)
//        {
//            Console.WriteLine($"start processing message {i}");
//            Console.WriteLine($"dl-message-dead-letter-message-id: {dlqMessage.MessageId}");
//            Console.WriteLine($"dl-message-dead-letter-reason: {dlqMessage.DeadLetterReason}");
//            Console.WriteLine($"dl-message-dead-letter-error-description: {dlqMessage.DeadLetterErrorDescription}");
//
//            ServiceBusMessage resubmittableMessage = new ServiceBusMessage(dlqMessage);
//
//            await sender.SendMessageAsync(resubmittableMessage);
//
//            await dlqReceiver.CompleteMessageAsync(dlqMessage);
//
//            Console.WriteLine($"finished processing message {i}");
//            Console.WriteLine("--------------------------------------------------------------------------------------");
//
//            i++;
//        }
//
//        await dlqReceiver.CloseAsync();
//
//        Console.WriteLine($"finished");
//    }
//}