using TicTacToe;

namespace Kurrent.Variant.Tests.Result;

public class ResultThenTests {
    Faker Faker { get; } = new();

    #region . Sync .

    [Test]
    public void then_chains_operation_when_called_on_success_result_returning_success() {
        // Arrange
        var initialSuccess = new GameId(Faker.Random.Guid());
        var result         = Kurrent.Result.Success<GameId, InvalidMoveError>(initialSuccess);
        var nextSuccess    = new GameUpdated(initialSuccess.Value, GameStatus.Ongoing);

        // Act
        var chainedResult = result.Then(gameId =>
            Kurrent.Result.Success<GameUpdated, InvalidMoveError>(nextSuccess)
        );

        // Assert
        chainedResult.IsSuccess.ShouldBeTrue();
        chainedResult.Value.ShouldBe(nextSuccess);
    }

    [Test]
    public void then_chains_operation_when_called_on_success_result_returning_error() {
        // Arrange
        var initialSuccess = new GameId(Faker.Random.Guid());
        var result         = Kurrent.Result.Success<GameId, InvalidMoveError>(initialSuccess);
        var nextError      = new InvalidMoveError(initialSuccess.Value, "Chained operation failed");

        // Act
        var chainedResult = result.Then(gameId =>
            Kurrent.Result.Failure<GameUpdated, InvalidMoveError>(nextError)
        );

        // Assert
        chainedResult.IsFailure.ShouldBeTrue();
        chainedResult.Error.ShouldBe(nextError);
    }

    [Test]
    public void then_returns_original_error_when_called_on_error_result() {
        // Arrange
        var initialError = new InvalidMoveError(Faker.Random.Guid(), "Initial operation failed");
        var result       = Kurrent.Result.Failure<GameId, InvalidMoveError>(initialError);

        // Act
        var chainedResult = result.Then(gameId =>
            Kurrent.Result.Success<GameUpdated, InvalidMoveError>(new GameUpdated(gameId.Value, GameStatus.Draw))
        );

        // Assert
        chainedResult.IsFailure.ShouldBeTrue();
        chainedResult.Error.ShouldBe(initialError);
    }

    [Test]
    public void then_passes_state_to_binder_when_using_stateful_variant() {
        // Arrange
        var initialSuccess       = new GameId(Faker.Random.Guid());
        var result               = Kurrent.Result.Success<GameId, InvalidMoveError>(initialSuccess);
        var contextState         = "Operation context";
        var nextSuccessWithState = new GameUpdated(initialSuccess.Value, GameStatus.Won, contextState);

        // Act
        var chainedResult = result.Then(
            (gameId, state) =>
                Kurrent.Result.Success<GameUpdated, InvalidMoveError>(new GameUpdated(gameId.Value, GameStatus.Won, state)),
            contextState
        );

        // Assert
        chainedResult.IsSuccess.ShouldBeTrue();
        chainedResult.Value.ShouldBe(nextSuccessWithState);
        chainedResult.Value.UpdateNotes.ShouldBe(contextState);
    }

    #endregion

    #region . Async .

    [Test]
    public async Task then_async_chains_operation_when_using_value_task_binder_returning_success() {
        // Arrange
        var initialSuccess = new GameId(Faker.Random.Guid());
        var result         = Kurrent.Result.Success<GameId, InvalidMoveError>(initialSuccess);
        var nextSuccess    = new GameUpdated(initialSuccess.Value, GameStatus.Ongoing);

        // Act
        var chainedResult = await result.ThenAsync(gameId =>
            ValueTask.FromResult(Kurrent.Result.Success<GameUpdated, InvalidMoveError>(nextSuccess))
        );

        // Assert
        chainedResult.IsSuccess.ShouldBeTrue();
        chainedResult.Value.ShouldBe(nextSuccess);
    }

    [Test]
    public async Task then_async_chains_operation_when_using_value_task_binder_returning_error() {
        // Arrange
        var initialSuccess = new GameId(Faker.Random.Guid());
        var result         = Kurrent.Result.Success<GameId, InvalidMoveError>(initialSuccess);
        var nextError      = new InvalidMoveError(initialSuccess.Value, "Async chained operation failed");

        // Act
        var chainedResult = await result.ThenAsync(gameId =>
            ValueTask.FromResult(Kurrent.Result.Failure<GameUpdated, InvalidMoveError>(nextError))
        );

        // Assert
        chainedResult.IsFailure.ShouldBeTrue();
        chainedResult.Error.ShouldBe(nextError);
    }

    [Test]
    public async Task then_async_propagates_original_error_when_called_on_error_result() {
        // Arrange
        var initialError = new InvalidMoveError(Faker.Random.Guid(), "Initial async operation failed");
        var result       = Kurrent.Result.Failure<GameId, InvalidMoveError>(initialError);

        // Act
        var chainedResult = await result.ThenAsync(gameId =>
            ValueTask.FromResult(Kurrent.Result.Success<GameUpdated, InvalidMoveError>(new GameUpdated(gameId.Value, GameStatus.Draw)))
        );

        // Assert
        chainedResult.IsFailure.ShouldBeTrue();
        chainedResult.Error.ShouldBe(initialError);
    }

    [Test]
    public async Task then_async_passes_state_to_binder_when_using_stateful_variant() {
        // Arrange
        var initialSuccess       = new GameId(Faker.Random.Guid());
        var result               = Kurrent.Result.Success<GameId, InvalidMoveError>(initialSuccess);
        var contextState         = "Async operation context";
        var nextSuccessWithState = new GameUpdated(initialSuccess.Value, GameStatus.Won, contextState);

        // Act
        var chainedResult = await result.ThenAsync(
            async (gameId, state) => {
                await Task.Delay(1); // Simulate async work
                return Kurrent.Result.Success<GameUpdated, InvalidMoveError>(new GameUpdated(gameId.Value, GameStatus.Won, state));
            }, contextState
        );

        // Assert
        chainedResult.IsSuccess.ShouldBeTrue();
        chainedResult.Value.ShouldBe(nextSuccessWithState);
        chainedResult.Value.UpdateNotes.ShouldBe(contextState);
    }

    [Test]
    public async Task then_async_handles_nested_async_operations_correctly() {
        // Arrange
        var initialSuccess = new GameId(Faker.Random.Guid());
        var result         = Kurrent.Result.Success<GameId, InvalidMoveError>(initialSuccess);

        Func<GameId, ValueTask<Result<GameUpdated, InvalidMoveError>>> firstOperation = async gameId => {
            await Task.Delay(1);
            return Kurrent.Result.Success<GameUpdated, InvalidMoveError>(new GameUpdated(gameId.Value, GameStatus.Ongoing));
        };

        Func<GameUpdated, ValueTask<Result<PlayerTurn, InvalidMoveError>>> secondOperation = async gameUpdate => {
            await Task.Delay(1);
            return Kurrent.Result.Success<PlayerTurn, InvalidMoveError>(new PlayerTurn(Player.X, new Position(0, 0), $"Turn for {gameUpdate.GameId}"));
        };

        // Act
        var intermediateResult = await result.ThenAsync(firstOperation);
        var finalResult        = await intermediateResult.ThenAsync(secondOperation);

        // Assert
        finalResult.IsSuccess.ShouldBeTrue();
        finalResult.Value.MoveDescription!.ShouldContain(initialSuccess.Value.ToString());
    }

    #endregion
}
