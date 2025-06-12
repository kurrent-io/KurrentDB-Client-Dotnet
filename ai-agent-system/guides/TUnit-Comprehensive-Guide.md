# TUnit Comprehensive Guide: Mastering the Art of .NET Testing

## Introduction to TUnit

TUnit is a modern, fast, and flexible .NET testing framework designed to enhance developer productivity and testing efficiency. It is built with a focus on performance, extensibility, and ease of use, offering a rich set of features that cater to a wide range of testing scenarios, from simple unit tests to complex integration and end-to-end tests. This guide provides a comprehensive overview of TUnit, with detailed explanations, numerous examples, and best practices to help you master the art of .NET testing, as if guided by its creator, Mr. Tom Longhurst.

## Installation and Setup

### Installing TUnit Project Templates

To get started with TUnit, you can install the official project templates, which streamline the setup process for new test projects.

```bash
dotnet new install TUnit.Templates
dotnet new TUnit -n "MyTestProject"
```

### Adding TUnit to an Existing Project

If you have an existing test project, you can add TUnit via NuGet.

```powershell
cd YourTestProjectNameHere
dotnet add package TUnit --prerelease
```

### .csproj Configuration

A minimal .csproj file for a TUnit project should look like this:

```xml
<Project Sdk="Microsoft.NET.Sdk">

    <PropertyGroup>
        <OutputType>Exe</OutputType>
        <TargetFramework>net8.0</TargetFramework>
        <ImplicitUsings>enable</ImplicitUsings>
        <Nullable>enable</Nullable>
    </PropertyGroup>

    <ItemGroup>
      <PackageReference Include="TUnit" Version="$(TUnitVersion)" />
    </ItemGroup>

</Project>
```

## Test Authoring

### Test Structure and Naming Conventions

TUnit tests are organized within classes, with each test method representing a single test case. Adhering to a clear and consistent naming convention is crucial for maintaining readability and understanding the purpose of each test.

```csharp
using TUnit.Core;

namespace MyTestProject;

public class MyTestClass
{
    [Test]
    public void MyTest()
    {
        // Test implementation
    }
}
```

### Data-Driven Testing

TUnit provides powerful attributes for data-driven testing, allowing you to run the same test method with multiple sets of input data.

#### Using `[Arguments]`

The `[Arguments]` attribute supplies a single set of arguments to a test method.

```csharp
[Test]
[Arguments(1, 2, 3)]
[Arguments(5, 10, 15)]
public void Calculate_Sum(int a, int b, int expected)
{
    var result = a + b;
    // Assertion logic here
}
```

#### Using `[Matrix]` and `[MatrixDataSource]`

For combinatorial testing, you can use the `[Matrix]` attribute on parameters and `[MatrixDataSource]` on the test method to generate all possible combinations of values.

```csharp
[Test]
[MatrixDataSource]
public void MyTest(
    [Matrix(1, 2, 3)] int value1,
    [Matrix(4, 5, 6)] int value2
)
{
    var result = value1 + value2;
    // Assertion logic here
}
```

## Test Lifecycle

TUnit offers a rich set of lifecycle hooks to manage setup and teardown operations at various scopes, ensuring that tests are run in a clean and consistent environment.

### `[Before]` and `[After]` Hooks

The `[Before]` and `[After]` attributes can be applied to methods to execute them before or after tests at different scopes.

```csharp
[Before(HookType.Test)]
public void BeforeEachTest()
{
    // Runs before each test in the class
}

[After(HookType.Class)]
public void AfterAllTestsInClass()
{
    // Runs after all tests in the class have completed
}
```

### `IAsyncInitializer` for Thread-Safe Initialization

For thread-safe initialization, especially in parallel testing scenarios, you can implement the `IAsyncInitializer` interface.

```csharp
public class WebAppFactory : WebApplicationFactory<Program>, IAsyncInitializer
{
    public Task InitializeAsync()
    {
        // This forces the server to initialize in a thread-safe manner
        _ = Server;
        return Task.CompletedTask;
    }
}
```

## Execution Control

TUnit provides fine-grained control over test execution, allowing you to manage retries, parallelism, and dependencies.

### Retrying Tests with `[Retry]`

The `[Retry]` attribute automatically retries a test a specified number of times on failure.

```csharp
[Test]
[Retry(3)]
public void MyFlakyTest()
{
    // Test implementation that might fail intermittently
}
```

### Controlling Parallelism with `[ParallelLimit]` and `[NotInParallel]`

You can control the degree of parallelism for your tests using `[ParallelLimit]` with a custom implementation of `IParallelLimit`.

```csharp
public class LoadTestParallelLimit : IParallelLimit
{
    public int Limit => 10; // Limit to 10 concurrent executions
}

[Test]
[ParallelLimit<LoadTestParallelLimit>]
public void Load_Test_Homepage()
{
    // Performance testing
}
```

To prevent tests from running in parallel, you can use the `[NotInParallel]` attribute.

```csharp
[Test]
[NotInParallel]
public void MyNonParallelTest()
{
    // Test implementation that should not run in parallel
}
```

### Managing Test Dependencies with `[DependsOn]`

The `[DependsOn]` attribute ensures that a test runs only after another specified test has completed successfully.

```csharp
[Test]
public void Register_User()
{
    // Test implementation for user registration
}

[Test]
[DependsOn(nameof(Register_User))]
public void Login_With_Registered_User()
{
    // This test runs after Register_User completes
}
```

## Examples & Use Cases

### Basic Test Case

A simple test case demonstrating the basic structure of a TUnit test.

```csharp
using TUnit.Core;

namespace MyTestProject;

public class MyTestClass
{
    [Test]
    public void MyTest()
    {
        var result = Add(1, 2);
        // Assertion logic here
    }

    private int Add(int x, int y)
    {
        return x + y;
    }
}
```

### ASP.NET Core Integration

TUnit can be integrated with ASP.NET Core for testing web applications.

```csharp
public class WebAppFactory : WebApplicationFactory<Program>, IAsyncInitializer
{
    public Task InitializeAsync()
    {
        _ = Server;
        return Task.CompletedTask;
    }
}
```

### Playwright Integration

TUnit supports browser automation testing with Playwright.

```csharp
public class Tests : PageTest
{
    [Test]
    public async Task Test()
    {
        await Page.GotoAsync("https://www.github.com/thomhurst/TUnit");
    }
}
```

### CI/CD Pipeline with Azure DevOps

You can integrate TUnit into your CI/CD pipeline with Azure DevOps to run tests and publish results.

```yaml
steps:
  - script: dotnet test --configuration Release -- --report-trx --results-directory $(Agent.TempDirectory)
    displayName: 'Run tests and output .trx file'
    continueOnError: true

  - task: PublishTestResults@2
    displayName: 'Publish Test Results from *.trx files'
    inputs:
      testResultsFormat: VSTest
      testResultsFiles: '*.trx'
      searchFolder: '$(Agent.TempDirectory)'
      failTaskOnFailedTests: true
      failTaskOnMissingResultsFile: true
```

## Running Tests

You can run TUnit tests using either `dotnet run` or `dotnet test`.

### Using `dotnet run`

```powershell
cd 'C:/Your/Test/Directory'
dotnet run -c Release
# or with flags
dotnet run -c Release -- --report-trx --coverage
```

### Using `dotnet test`

```powershell
cd 'C:/Your/Test/Directory'
dotnet test -c Release
# or with flags
dotnet test -c Release -- --report-trx --coverage
```

## Best Practices

- **Clear Naming**: Use descriptive names for your tests to clearly indicate their purpose.
- **Isolate Tests**: Ensure that tests are independent and do not rely on the state of other tests.
- **Use Lifecycle Hooks**: Leverage lifecycle hooks for setup and teardown to keep your tests clean and maintainable.
- **Data-Driven Testing**: Use data-driven testing to cover multiple scenarios with a single test method.
- **Control Execution**: Use execution control attributes to manage retries, parallelism, and dependencies effectively.
