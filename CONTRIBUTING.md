# How to contribute

## Packing

Building the project  will `dotnet pack` the tool into the `/nupkg` folder.

## Installing a dev build

Run the following command from the project root to install the latest build:

`dotnet tool install AzureServiceBusDLQ --global --prerelease --add-source ./nupkg`

## Running tests

Make sure that the environment variable `AzureServiceBusDLQ_ConnectionString` is set to a valid Azure ServiceBus namespace connection string.
