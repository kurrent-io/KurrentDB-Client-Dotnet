# Build and Run

This document provides instructions on how to build, test, and run the KurrentDB .NET Client.

## Prerequisites

- [.NET SDK](https://dotnet.microsoft.com/download): The project targets .NET 8.0 and .NET 9.0. You will need to have the appropriate SDKs installed.
- [Docker](https://www.docker.com/products/docker-desktop): Docker is required for running integration tests, as they rely on a KurrentDB instance running in a container.

## Building the Project

To build the solution, navigate to the root of the repository and run the following command:

```bash
dotnet build
```

This will build all the projects in the solution.

## Running Tests

The project has a comprehensive test suite that includes unit, integration, and performance tests. To run all the tests, use the following command:

```bash
dotnet test
```

### Running Integration Tests

The integration tests require a running instance of KurrentDB. The tests are configured to use Docker to automatically spin up a KurrentDB container. Ensure that Docker is running before executing the tests.

## Running the Samples

The `samples` directory contains a number of sample applications that demonstrate how to use the KurrentDB .NET Client. To run a sample, navigate to its directory and use the `dotnet run` command.

For example, to run the `quick-start` sample:

```bash
cd samples/quick-start
dotnet run
```

Each sample is a self-contained project that can be run independently.
