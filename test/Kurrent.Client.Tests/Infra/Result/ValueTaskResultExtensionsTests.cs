using Bogus;

namespace Kurrent.Client.Tests.Infra.Result;

public class ValueTaskResultExtensionsTests {
    Faker Faker { get; } = new();

    [Test]
    public async Task as_result_async_returns_success_when_task_succeeds() {
        // Arrange
        var expectedValue = new GameId(Faker.Random.Guid());
        var task          = new ValueTask<GameId>(expectedValue);

        // Act
        var result = await task.ToResultAsync(ex => ex.Message);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(expectedValue);
    }

    [Test]
    public async Task as_result_async_returns_error_when_task_faults() {
        // Arrange
        var errorMessage = "Task failed";
        var task         = new ValueTask<GameId>(Task.FromException<GameId>(new InvalidOperationException(errorMessage)));

        // Act
        var result = await task.ToResultAsync(ex => ex.Message);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(errorMessage);
    }

    [Test]
    public async Task as_result_async_for_unit_returns_success_when_task_succeeds() {
        // Arrange
        var task = new ValueTask();

        // Act
        var result = await task.ToResultAsync(ex => ex.Message);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(Void.Value);
    }

    [Test]
    public async Task as_result_async_for_unit_returns_error_when_task_faults() {
        // Arrange
        var errorMessage = "Unit task failed";
        var task         = new ValueTask(Task.FromException(new InvalidOperationException(errorMessage)));

        // Act
        var result = await task.ToResultAsync(ex => ex.Message);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(errorMessage);
    }
}
