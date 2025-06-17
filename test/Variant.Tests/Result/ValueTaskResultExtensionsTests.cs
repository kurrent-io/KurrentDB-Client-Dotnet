namespace Kurrent.Variant.Tests.Result;

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

    #region . SwitchAsync Tests .

    [Test]
    public async Task switch_async_with_sync_actions_executes_success_action_when_result_is_success() {
        // Arrange
        var value = new GameId(Faker.Random.Guid());
        var task = new ValueTask<Result<GameId, string>>(Kurrent.Result.Success<GameId, string>(value));
        var successCalled = false;
        var errorCalled = false;

        // Act
        await task.SwitchAsync(
            _ => { successCalled = true; },
            _ => { errorCalled = true; }
        );

        // Assert
        successCalled.ShouldBeTrue();
        errorCalled.ShouldBeFalse();
    }

    [Test]
    public async Task switch_async_with_sync_actions_executes_error_action_when_result_is_error() {
        // Arrange
        var error = "Something went wrong";
        var task = new ValueTask<Result<GameId, string>>(Kurrent.Result.Failure<GameId, string>(error));
        var successCalled = false;
        var errorCalled = false;

        // Act
        await task.SwitchAsync(
            _ => { successCalled = true; },
            _ => { errorCalled = true; }
        );

        // Assert
        successCalled.ShouldBeFalse();
        errorCalled.ShouldBeTrue();
    }

    [Test]
    public async Task switch_async_with_async_actions_executes_success_action_when_result_is_success() {
        // Arrange
        var value = new GameId(Faker.Random.Guid());
        var task = new ValueTask<Result<GameId, string>>(Kurrent.Result.Success<GameId, string>(value));
        var successCalled = false;
        var errorCalled = false;

        // Act
        await task.SwitchAsync(
            async _ => {
                await Task.Delay(1);
                successCalled = true;
            },
            async _ => {
                await Task.Delay(1);
                errorCalled = true;
            }
        );

        // Assert
        successCalled.ShouldBeTrue();
        errorCalled.ShouldBeFalse();
    }

    [Test]
    public async Task switch_async_with_async_actions_executes_error_action_when_result_is_error() {
        // Arrange
        var error         = "Something went wrong";
        var task          = new ValueTask<Result<GameId, string>>(Kurrent.Result.Failure<GameId, string>(error));
        var successCalled = false;
        var errorCalled   = false;

        // Act
        await task.SwitchAsync(
            async _ => {
                await Task.Delay(1);
                successCalled = true;
            },
            async _ => {
                await Task.Delay(1);
                errorCalled = true;
            }
        );

        // Assert
        successCalled.ShouldBeFalse();
        errorCalled.ShouldBeTrue();
    }

    [Test]
    public async Task switch_async_with_mixed_actions_sync_success_async_error_executes_success_action() {
        // Arrange
        var value = new GameId(Faker.Random.Guid());
        var task = new ValueTask<Result<GameId, string>>(Kurrent.Result.Success<GameId, string>(value));
        var successCalled = false;
        var errorCalled = false;

        // Act
        await task.SwitchAsync(
            _ => { successCalled = true; },
            async _ => {
                await Task.Delay(1);
                errorCalled = true;
            }
        );

        // Assert
        successCalled.ShouldBeTrue();
        errorCalled.ShouldBeFalse();
    }

    [Test]
    public async Task switch_async_with_mixed_actions_async_success_sync_error_executes_error_action() {
        // Arrange
        var error         = "Something went wrong";
        var task          = new ValueTask<Result<GameId, string>>(Kurrent.Result.Failure<GameId, string>(error));
        var successCalled = false;
        var errorCalled   = false;

        // Act
        await task.SwitchAsync(
            async _ => {
                await Task.Delay(1);
                successCalled = true;
            },
            _ => { errorCalled = true; }
        );

        // Assert
        successCalled.ShouldBeFalse();
        errorCalled.ShouldBeTrue();
    }

    [Test]
    public async Task switch_async_with_state_executes_success_action_with_correct_state() {
        // Arrange
        var value = new GameId(Faker.Random.Guid());
        var task = new ValueTask<Result<GameId, string>>(Kurrent.Result.Success<GameId, string>(value));
        var state = "test-state";
        var receivedState = "";
        var receivedValue = Guid.Empty;

        // Act
        await task.SwitchAsync(
            (val, st) => {
                receivedValue = val.Value;
                receivedState = st;
            },
            (_, _) => { },
            state
        );

        // Assert
        receivedValue.ShouldBe(value.Value);
        receivedState.ShouldBe(state);
    }

    [Test]
    public async Task switch_async_with_state_executes_error_action_with_correct_state() {
        // Arrange
        var error         = "Something went wrong";
        var task          = new ValueTask<Result<GameId, string>>(Kurrent.Result.Failure<GameId, string>(error));
        var state         = "test-state";
        var receivedState = "";
        var receivedError = "";

        // Act
        await task.SwitchAsync(
            (_, _) => { },
            (err, st) => {
                receivedError = err;
                receivedState = st;
            },
            state
        );

        // Assert
        receivedError.ShouldBe(error);
        receivedState.ShouldBe(state);
    }

    [Test]
    public async Task switch_async_with_async_state_actions_executes_success_action_with_correct_state() {
        // Arrange
        var value = new GameId(Faker.Random.Guid());
        var task = new ValueTask<Result<GameId, string>>(Kurrent.Result.Success<GameId, string>(value));
        var state = "test-state";
        var receivedState = "";
        var receivedValue = Guid.Empty;

        // Act
        await task.SwitchAsync(
            async (val, st) => {
                await Task.Delay(1);
                receivedValue = val.Value;
                receivedState = st;
            },
            async (_, _) => { await Task.Delay(1); },
            state
        );

        // Assert
        receivedValue.ShouldBe(value.Value);
        receivedState.ShouldBe(state);
    }

    #endregion
}
