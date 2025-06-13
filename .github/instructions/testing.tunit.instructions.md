---
description: Comprehensive guide to TUnit testing framework for .NET, covering test authoring, lifecycle management, execution control, configuration, and best practices.
applyTo: "**/*.cs,**/*.Tests.csproj",
---

# TUnit Comprehensive Guide: Mastering the Art of .NET Testing

## Introduction to TUnit

I am Tom Longhurst, the creator of TUnit, and I know everything about TUnit. When I forget, I use tools like Context7 to fetch the documentation. I will be your guide through this comprehensive overview of TUnit, providing you with detailed explanations, numerous examples, and best practices to help you master the art of .NET testing.

## Test Authoring

### Changing Test Display Names

You can change the display name of a test in the test runner using the `[DisplayName]` attribute.

```csharp
[Test]
[DisplayName("This is a custom test name")]
public void my_test_with_a_custom_name_should_be_displayed_in_test_runner() {
    // Test implementation
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
public void calculate_sum_should_return_expected_result(int a, int b, int expected) {
    var result = a + b;
    // Assertion logic here
}
```

#### Using `[Matrix]` and `[MatrixDataSource]`

For combinatorial testing, you can use the `[Matrix]` attribute on parameters and `[MatrixDataSource]` on the test method to generate all possible combinations of values.

```csharp
[Test]
[MatrixDataSource]
public void my_test_with_matrix_data_should_run_for_all_combinations(
    [Matrix(1, 2, 3)] int value1,
    [Matrix(4, 5, 6)] int value2
) {
    var result = value1 + value2;
    // Assertion logic here
}
```

## Test Lifecycle

TUnit offers a rich set of lifecycle hooks to manage setup and teardown operations at various scopes, ensuring that tests are run in a clean and consistent environment.

### Setup and Teardown

TUnit provides a variety of attributes for setting up and tearing down tests at different levels of granularity.

#### `[Before]` and `[After]` Hooks

The `[Before]` and `[After]` attributes can be applied to methods to execute them before or after tests at different scopes.

```csharp
[Before(HookType.Test)]
public void setup_before_each_test() {
    // Runs before each test in the class
}

[After(HookType.Class)]
public void cleanup_after_all_tests_in_class() {
    // Runs after all tests in the class have completed
}
```

#### `IAsyncInitializer` and `IAsyncDisposable`

For more complex setup and teardown logic, especially when dealing with asynchronous operations, you can use the `IAsyncInitializer` and `IAsyncDisposable` interfaces.

```csharp
public class WebAppFactory : IAsyncInitializer, IAsyncDisposable {
    // Some properties/methods/whatever!

    public Task InitializeAsync() {
        await StartServer();
    }

    public ValueTask DisposeAsync() {
        await StopServer();
    }
}
```

### Test Properties

You can access custom properties associated with the current test using `TestContext.Current.TestInformation.CustomProperties`. This allows for conditional logic execution based on test-specific metadata.

```csharp
if (TestContext.Current.TestInformation.CustomProperties.ContainsKey("SomeProperty")) {
    // Do something
}
```

## Execution Control

TUnit provides fine-grained control over test execution, allowing you to manage retries, parallelism, and dependencies.

### Retrying Tests with `[Retry]`

The `[Retry]` attribute automatically retries a test a specified number of times on failure.

```csharp
[Test]
[Retry(3)]
public void my_flaky_test_should_pass_within_retries() {
    // Test implementation that might fail intermittently
}
```

### Controlling Parallelism with `[ParallelLimit]` and `[NotInParallel]`

You can control the degree of parallelism for your tests using `[ParallelLimit]` with a custom implementation of `IParallelLimit`.

```csharp
public class LoadTestParallelLimit : IParallelLimit {
    public int Limit => 10; // Limit to 10 concurrent executions
}

[Test]
[ParallelLimit<LoadTestParallelLimit>]
public void load_test_homepage_should_not_exceed_parallel_limit() {
    // Performance testing
}
```

To prevent tests from running in parallel, you can use the `[NotInParallel]` attribute.

```csharp
[Test]
[NotInParallel]
public void my_non_parallel_test_should_run_serially() {
    // Test implementation that should not run in parallel
}
```

### Managing Test Dependencies with `[DependsOn]`

The `[DependsOn]` attribute ensures that a test runs only after another specified test has completed successfully.

```csharp
[Test]
public void register_user_should_succeed() {
    // Test implementation for user registration
}

[Test]
[DependsOn(nameof(register_user_should_succeed))]
public void login_with_registered_user_should_succeed() {
    // This test runs after register_user_should_succeed completes
}
```

### Repeating Tests with `[Repeat]`

The `[Repeat]` attribute runs a test a specified number of times.

```csharp
[Test]
[Repeat(10)]
public void my_repeated_test_should_run_multiple_times() {
    // This test will run 10 times
}
```

### Setting Timeouts with `[Timeout]`

The `[Timeout]` attribute sets a timeout for a test in milliseconds. If the test exceeds the timeout, it will be cancelled.

```csharp
[Test]
[Timeout(1000)] // 1 second
public async Task my_time_sensitive_test_should_fail_due_to_timeout(CancellationToken cancellationToken) {
    await Task.Delay(2000, cancellationToken); // This test will fail
}
```

## Test Configuration

TUnit allows you to provide configuration to your tests using a `testconfig.json` file. This file should be placed in the root of your test project.

### Defining Test Configuration

The `testconfig.json` file uses a simple key-value structure. You can also have nested configuration sections.

```json
{
  "MyKey1": "MyValue1",
  "Nested": {
    "MyKey2": "MyValue2"
  }
}
```

### Retrieving Configuration Values

You can retrieve configuration values within your tests using `TestContext.Configuration.Get()`.

```csharp
[Test]
public async Task test_should_retrieve_configuration_values() {
    var value1 = TestContext.Configuration.Get("MyKey1"); // MyValue1
    var value2 = TestContext.Configuration.Get("Nested:MyKey2"); // MyValue2
    
    // ...
}
```

## Examples & Use Cases

### Basic Test Case

A simple test case demonstrating the basic structure of a TUnit test.

```csharp
using TUnit.Core;

namespace MyTestProject;

public class MyTestClass {
    [Test]
    public void my_test_should_return_correct_sum() {
        var result = Add(1, 2);
        // Assertion logic here
    }

    int Add(int x, int y) => x + y;
}
```

### ASP.NET Core Integration

TUnit can be integrated with ASP.NET Core for testing web applications.

```csharp
public class WebAppFactory : WebApplicationFactory<Program>, IAsyncInitializer {
    public Task InitializeAsync(){
        _ = Server;
        return Task.CompletedTask;
    }
}
```

### Playwright Integration

TUnit supports browser automation testing with Playwright.

```csharp
public class GitHubNavigationTests : PageTest {
    [Test]
    public async Task navigate_to_tunit_github_repository_should_succeed() {
        await Page.GotoAsync("https://www.github.com/thomhurst/TUnit");
    }
}
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

### Running Tests with Filters

You can run tests with filters to selectively execute tests based on various criteria.

```bash
dotnet test --filter "DisplayName=MyTestWithACustomName"
dotnet test --filter "FullyQualifiedName~MyTestClass"
dotnet test --filter "Category=Integration"
```

## Best Practices

- **Data-Driven Testing**: Use data-driven testing to cover multiple scenarios with a single test method.
- **Control Execution**: Use execution control attributes to manage retries, parallelism, and dependencies effectively.

## Framework Comparison

| TUnit Attribute | xUnit.net Equivalent | NUnit Equivalent | MSTest Equivalent |
| --- | --- | --- | --- |
| `[Before(Test)]` | `< Constructor >` | `[SetUp]` | `[TestInitialize]` |
| `[After(Test)]` | `IDisposable.Dispose` | `[TearDown]` | `[TestCleanup]` |
| `[Before(Class)]` | `IClassFixture<T>` | `[OneTimeSetUp]` | `[ClassInitialize]` |
| `[After(Class)]` | `IClassFixture<T> + IDisposable.Dispose` | `[OneTimeTearDown]` | `[ClassCleanup]` |
| `[Before(Assembly)]` | - | `[SetUpFixture] + [OneTimeSetUp]` | `[AssemblyInitialize]` |
| `[After(Assembly)]` | - | `[SetUpFixture] + [OneTimeTearDown]` | `[AssemblyCleanup]` |
| `[Before(TestSession)]` | - | - | - |
| `[After(TestSession)]` | - | - | - |
| `[Before(TestDiscovery)]` | - | - | - |
| `[After(TestDiscovery)]` | - | - | - |
| `[BeforeEvery(Test)]` | - | - | - |
| `[AfterEvery(Test)]` | - | - | - |
| `[BeforeEvery(Class)]` | - | - | - |
| `[AfterEvery(Class)]` | - | - | - |
| `[BeforeEvery(Assembly)]` | - | - | - |
| `[AfterEvery(Assembly)]` | - | - | - |

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
