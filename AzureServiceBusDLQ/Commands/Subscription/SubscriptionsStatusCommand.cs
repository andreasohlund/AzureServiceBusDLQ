using System.ComponentModel;
using Azure.Identity;
using Azure.Messaging.ServiceBus.Administration;
using Spectre.Console;
using Spectre.Console.Cli;

public class SubscriptionsStatusCommand(ServiceBusAdministrationClient administrationClient) : CancellableAsyncCommand<BaseSettings>
{
    protected override async Task<int> ExecuteAsync(CommandContext context, BaseSettings settings, CancellationToken cancellationToken)
    {
        await AnsiConsole.Progress()
            .Columns(new TaskDescriptionColumn(), new SpinnerColumn())
            .HideCompleted(true)
            .StartAsync(async ctx =>
            {
                var subscriptionProgress = ctx.AddTask($"Fetching subscription DLQ status");

                var subscriptions = await GetSubscriptionRuntimeProperties(administrationClient, subscriptionProgress, cancellationToken);

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

                AnsiConsole.Write(subscriptionTable);
            });


        return 0;
    }

    async Task<List<SubscriptionRuntimeProperties>> GetSubscriptionRuntimeProperties(ServiceBusAdministrationClient client, ProgressTask progressTask, CancellationToken cancellationToken)
    {
        var subscriptions = new List<SubscriptionRuntimeProperties>();

        await foreach (var topic in client.GetTopicsAsync(cancellationToken))
        {
            await foreach (var queue in client.GetSubscriptionsRuntimePropertiesAsync(topic.Name, cancellationToken))
            {
                subscriptions.Add(queue);
                progressTask.Increment(1);
            }
        }

        progressTask.StopTask();

        return subscriptions;
    }
}