using System.ComponentModel;
using Azure.Identity;
using Azure.Messaging.ServiceBus.Administration;
using Spectre.Console;
using Spectre.Console.Cli;

public class SubscriptionsStatusCommand : CancellableAsyncCommand<SubscriptionsStatusCommand.Settings>
{
    public class Settings : BaseSettings
    {
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        await AnsiConsole.Progress()
            .Columns(new TaskDescriptionColumn(), new SpinnerColumn())
            .HideCompleted(true)
            .StartAsync(async ctx =>
            {
                var queuesProgress = ctx.AddTask($"Fetching queue DLQ status");
                var subscriptionProgress = ctx.AddTask($"Fetching subscription DLQ status");

                var queues =  await GetQueueRuntimeProperties(settings.AdministrationClient, queuesProgress, cancellationToken);
                var subscriptions =  await GetSubscriptionRuntimeProperties(settings.AdministrationClient, subscriptionProgress, cancellationToken);

                var queueTable = new Table
                {
                    Title = new TableTitle("Queue DLQ Status")
                };

                queueTable.AddColumn("Queue");
                queueTable.AddColumn(new TableColumn("Count").Centered());
                
                foreach (var queue in queues)
                {
                    queueTable.AddRow(queue.Name, $"{queue.DeadLetterMessageCount.ToString()}:{queue.TransferDeadLetterMessageCount.ToString()}");
                }
                
                var subscriptionTable = new Table
                {
                    Title = new TableTitle("Subscription DLQ Status")
                };

                subscriptionTable.AddColumn("Subscription");
                subscriptionTable.AddColumn(new TableColumn("Count").Centered());
                
                foreach (var subscription in subscriptions)
                {
                    subscriptionTable.AddRow(subscription.SubscriptionName, $"{subscription.DeadLetterMessageCount.ToString()}:{subscription.TransferDeadLetterMessageCount.ToString()}");
                }
                
                AnsiConsole.Write(queueTable);
                AnsiConsole.Write(subscriptionTable);
            });

     

        return 0;
    }

    async Task<List<QueueRuntimeProperties>> GetQueueRuntimeProperties(ServiceBusAdministrationClient client, ProgressTask progressTask, CancellationToken cancellationToken)
    {
        var queues = new List<QueueRuntimeProperties>();

        await foreach (var queue in client.GetQueuesRuntimePropertiesAsync(cancellationToken))
        {
            queues.Add(queue);
            progressTask.Increment(1);
        }

        progressTask.StopTask();

        return queues;
    }
    
    async Task<List<SubscriptionRuntimeProperties>> GetSubscriptionRuntimeProperties(ServiceBusAdministrationClient client, ProgressTask progressTask, CancellationToken cancellationToken)
    {
        var subscriptions = new List<SubscriptionRuntimeProperties>();

        await foreach (var topic in client.GetTopicsAsync(cancellationToken))
        {
            await foreach (var queue in client.GetSubscriptionsRuntimePropertiesAsync(topic.Name,cancellationToken))
            {
                subscriptions.Add(queue);
                progressTask.Increment(1);
            }
        }
        
        progressTask.StopTask();

        return subscriptions;
    }
}