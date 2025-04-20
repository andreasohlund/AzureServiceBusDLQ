# AzureServiceBusDLQ

## Installing

`dotnet tool install AzureServiceBusDLQ --global`

## Connecting

> [!NOTE]  
> [Listen claims](https://learn.microsoft.com/en-us/azure/service-bus-messaging/service-bus-authentication-and-authorization) is required.

Connecting to the azure service bus namespace can be done in two ways:

- Using a connection string: `-c {my connection string}`
- Using a namespace name: `-n {my namespace}`
  - In this mode `DefaultAzureCredentials` will be used to authenticate

## Listing queues with DLQ/TDLQ messages

In this example using the namespace name:

`asb-dlq queues -n my-asb-namespace`

Outputs:

- All queues with at least one DLQ or TDLQ message.

Returns:

- `0` if no queues contains DLQ/TDLQ messages
- `1` if at least one queue have DLQ/TDLQ messages

## Show DLQ/TDLQ messages for a specific queue

In this example using the namespace name:

`asb-dlq queue my-queue -n my-asb-namespace`

Outputs:

- Details of all DLQ and TDLQ messages for the specified queue.

- Returns:

- `0` if no DLQ/TDLQ messages exists
- `1` if at least one DLQ/TDLQ message exists

## Retry DLQ messages for a specific queue

Retries DLQ messages by moving them back to the parent queue.

> [!NOTE]  
> [Transactions](https://learn.microsoft.com/en-us/azure/service-bus-messaging/service-bus-transactions) are used to ensure that retries are atomic.

In this example using the namespace name:

`asb-dlq retry-queue my-queue -n my-asb-namespace`

Outputs:

- Message details for retried messages.

- Returns:

- `0` if messages where retried successfully or if no DLQ messages where found.
- `1` if DLQ messages failed to be retried

## Move DLQ messages

Moves DLQ messages to the target queue

> [!NOTE]  
> [Transactions](https://learn.microsoft.com/en-us/azure/service-bus-messaging/service-bus-transactions) are used to ensure that the move is atomic.

Move single queue with default transformation (see below):

`asb-dlq move-dlq-messages my-source-queue my-target-queue -n my-asb-namespace`

Use NServiceBus transformation:

`asb-dlq move-dlq-messages my-source-queue my-target-queue --transform nservicebus -n my-asb-namespace`

Outputs:

- Message details for moved messages.

- Returns:

- `0` if messages where moved successfully or if no DLQ messages where found.
- `1` if DLQ messages failed to be moved

### Transforms

The move operation will capture metadata about the DLQ message via transformations. The following transformations are available.

#### Default

Adds the following application properties to the moved message:

- `x-asb-dlq-source-queue`: Queue name where the message was dead lettered
- `x-asb-dlq-reason`: Dead letter reason
- `x-asb-dlq-description`: Dead letter description

#### NServiceBus

Adds the following application properties to the moved message:

- `NServiceBus.FailedQ`: Queue name where the message was dead lettered
- `NServiceBus.ProcessingEndpoint`: Queue name where the message was dead lettered
- `NServiceBus.TimeOfFailure`: Time when the message was dead lettered. NOTE: For now this is set to utc now.
- `NServiceBus.ExceptionInfo.ExceptionType`: Dead letter reason
- `NServiceBus.ExceptionInfo.Message`: Dead letter description
- `NServiceBus.MessageId`: Unless already present the native Azure ServiceBus message id will be used as the message id

#### None

No modifications are done to the moved message.
