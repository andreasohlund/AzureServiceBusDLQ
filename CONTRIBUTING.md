# How to contribute

## Packing

Use `dotnet pack` to create the nuget package with the tool in the `/nupkg` xfolder.

## Running tests

Make sure that the environment variable `AzureServiceBusDLQ_ConnectionString` is set to a valid Azure ServiceBus namespace connectionstring.