# AzureServiceBusDLQ

## Installing

Use `dotnet tool install AzureServiceBusDLQ --global --prerelease` install the tool.

## Connecting

Connecting to the azure service bus namespace can be done in two ways:

- Using a connection string: `-c {my connection string}`
- Using a namespace name: `-n {my namespace}`
  - In this mode `DefaultAzureCredentials` will be used to authenticate

## Listing queues with DLQ/TDLQ messages

In this example using the namespace name:

`asb-dlq queues -n my-asb-namespace`

Returns:

- `0` if no queues contains DLQ/TDLQ messages
- `1` if at least one queue have DLQ/TDLQ messages

## Show DLQ/TDLQ messages for a specific queue

In this example using the namespace name:

> [!NOTE]  
> [Listen claims](https://learn.microsoft.com/en-us/azure/service-bus-messaging/service-bus-authentication-and-authorization) is needed to view DLQ messages

`asb-dlq queue my-queue -n my-asb-namespace`

Returns:

- `0` if no DLQ/TDLQ messages exists
- `1` if at least one DLQ/TDLQ message exists
