# AzureServiceBusDLQ

## Connecting

Connecting to the azure service bus namespace can be done in two ways:

- Using a connection string: `-c {my connection string}`
- Using a namespace name: `-n {my namespace}`
  - In this mode `DefaultAzureCredentials` will be used to authenticate