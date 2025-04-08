# AzureServiceBusDLQ

## Installing

Use `dotnet tool install --global AzureServiceBusDLQ --version 1.0.0-alpha.0.3` install the tool.

## Connecting

Connecting to the azure service bus namespace can be done in two ways:

- Using a connection string: `-c {my connection string}`
- Using a namespace name: `-n {my namespace}`
  - In this mode `DefaultAzureCredentials` will be used to authenticate

## Listing queues with DLQ messages

In this example using the namespace name:

`asb-dlq queues -n my-asb-namespace`
