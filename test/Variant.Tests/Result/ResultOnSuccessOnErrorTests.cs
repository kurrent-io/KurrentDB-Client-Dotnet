using TicTacToe;

namespace Kurrent.Variant.Tests.Result;

public class ResultOnSuccessOnErrorTests {
    Faker Faker { get; } = new();

    #region . Sync .

    [Test]
    public void on_success_executes_action_and_returns_same_instance_when_called_on_success_result() {
        // Arrange
        GameStarted successValue = new GameStarted(Faker.Random.Guid(), Faker.PickRandom<Player>());
        Result<GameStarted, InvalidMoveError> originalResult = Kurrent.Result.Success<GameStarted, InvalidMoveError>(successValue);
        bool actionExecuted = false;

        // Act
        Result<GameStarted, InvalidMoveError> returnedResult = originalResult.OnSuccess(_ => actionExecuted = true);

        // Assert
        actionExecuted.ShouldBeTrue();
        returnedResult.ShouldBeEquivalentTo(originalResult);
    }

    [Test]
    public void on_success_does_not_execute_action_and_returns_same_instance_when_called_on_error_result() {
        // Arrange
        InvalidMoveError errorValue = new InvalidMoveError(Faker.Random.Guid(), Faker.Lorem.Sentence());
        Result<GameStarted, InvalidMoveError> originalResult = Kurrent.Result.Failure<GameStarted, InvalidMoveError>(errorValue);
        bool actionExecuted = false;

        // Act
        Result<GameStarted, InvalidMoveError> returnedResult = originalResult.OnSuccess(_ => actionExecuted = true);

        // Assert
        actionExecuted.ShouldBeFalse();
        returnedResult.ShouldBeEquivalentTo(originalResult);
    }

    [Test]
    public void on_error_executes_action_and_returns_same_instance_when_called_on_error_result() {
        // Arrange
        InvalidMoveError errorValue = new InvalidMoveError(Faker.Random.Guid(), Faker.Lorem.Sentence());
        Result<GameStarted, InvalidMoveError> originalResult = Kurrent.Result.Failure<GameStarted, InvalidMoveError>(errorValue);
        bool actionExecuted = false;

        // Act
        Result<GameStarted, InvalidMoveError> returnedResult = originalResult.OnError(_ => actionExecuted = true);

        // Assert
        actionExecuted.ShouldBeTrue();
        returnedResult.ShouldBeEquivalentTo(originalResult);
    }

    [Test]
    public void on_error_does_not_execute_action_and_returns_same_instance_when_called_on_success_result() {
        // Arrange
        GameStarted successValue = new GameStarted(Faker.Random.Guid(), Faker.PickRandom<Player>());
        Result<GameStarted, InvalidMoveError> originalResult = Kurrent.Result.Success<GameStarted, InvalidMoveError>(successValue);
        bool actionExecuted = false;

        // Act
        Result<GameStarted, InvalidMoveError> returnedResult = originalResult.OnError(_ => actionExecuted = true);

        // Assert
        actionExecuted.ShouldBeFalse();
        returnedResult.ShouldBeEquivalentTo(originalResult);
    }

    [Test]
    public void on_success_passes_state_to_action_when_using_stateful_variant() {
        // Arrange
        GameStarted successValue = new GameStarted(Faker.Random.Guid(), Faker.PickRandom<Player>());
        Result<GameStarted, InvalidMoveError> originalResult = Kurrent.Result.Success<GameStarted, InvalidMoveError>(successValue);
        string contextState = "Audit trail";
        string? capturedState = null;

        // Act
        originalResult.OnSuccess((gs, state) => capturedState = $"{state}: {gs.GameId}", contextState);

        // Assert
        capturedState.ShouldBe($"{contextState}: {successValue.GameId}");
    }

    [Test]
    public void on_error_passes_state_to_action_when_using_stateful_variant() {
        // Arrange
        InvalidMoveError errorValue = new InvalidMoveError(Faker.Random.Guid(), Faker.Lorem.Sentence());
        Result<GameStarted, InvalidMoveError> originalResult = Kurrent.Result.Failure<GameStarted, InvalidMoveError>(errorValue);
        string contextState = "Error context";
        string? capturedState = null;

        // Act
        originalResult.OnError((err, state) => capturedState = $"{state}: {err.Reason}", contextState);

        // Assert
        capturedState.ShouldBe($"{contextState}: {errorValue.Reason}");
    }

    #endregion

    #region . Async .

    [Test]
    public async Task on_success_async_executes_action_with_value_task() {
        // Arrange
        GameStarted successValue = new GameStarted(Faker.Random.Guid(), Faker.PickRandom<Player>());
        Result<GameStarted, InvalidMoveError> originalResult = Kurrent.Result.Success<GameStarted, InvalidMoveError>(successValue);
        bool actionExecuted = false;

        // Act
        Result<GameStarted, InvalidMoveError> returnedResult = await originalResult.OnSuccessAsync(async _ => {
            await Task.Delay(1);
            actionExecuted = true;
        });

        // Assert
        actionExecuted.ShouldBeTrue();
        returnedResult.ShouldBeEquivalentTo(originalResult);
    }

    [Test]
    public async Task on_error_async_executes_action_with_value_task() {
        // Arrange
        InvalidMoveError errorValue = new InvalidMoveError(Faker.Random.Guid(), Faker.Lorem.Sentence());
        Result<GameStarted, InvalidMoveError> originalResult = Kurrent.Result.Failure<GameStarted, InvalidMoveError>(errorValue);
        bool actionExecuted = false;

        // Act
        Result<GameStarted, InvalidMoveError> returnedResult = await originalResult.OnErrorAsync(async _ => {
            await Task.Delay(1);
            actionExecuted = true;
        });

        // Assert
        actionExecuted.ShouldBeTrue();
        returnedResult.ShouldBeEquivalentTo(originalResult);
    }

    [Test]
    public async Task on_success_async_passes_state_to_action_when_using_stateful_variant() {
        // Arrange
        GameStarted successValue = new GameStarted(Faker.Random.Guid(), Faker.PickRandom<Player>());
        Result<GameStarted, InvalidMoveError> originalResult = Kurrent.Result.Success<GameStarted, InvalidMoveError>(successValue);
        string contextState = "Async audit trail";
        string? capturedState = null;

        // Act
        await originalResult.OnSuccessAsync(async (gs, state) => {
            await Task.Delay(1);
            capturedState = $"{state}: {gs.GameId}";
        }, contextState);

        // Assert
        capturedState.ShouldBe($"{contextState}: {successValue.GameId}");
    }

    [Test]
    public async Task on_error_async_passes_state_to_action_when_using_stateful_variant() {
        // Arrange
        InvalidMoveError errorValue = new InvalidMoveError(Faker.Random.Guid(), Faker.Lorem.Sentence());
        Result<GameStarted, InvalidMoveError> originalResult = Kurrent.Result.Failure<GameStarted, InvalidMoveError>(errorValue);
        string contextState = "Async error context";
        string? capturedState = null;

        // Act
        await originalResult.OnErrorAsync(async (err, state) => {
            await Task.Delay(1);
            capturedState = $"{state}: {err.Reason}";
        }, contextState);

        // Assert
        capturedState.ShouldBe($"{contextState}: {errorValue.Reason}");
    }

    #endregion
}
