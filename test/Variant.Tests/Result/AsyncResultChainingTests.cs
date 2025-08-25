using TicTacToe;

namespace Kurrent.Variant.Tests.Result;

public class AsyncResultChainingTests {
    Faker Faker { get; } = new();

    #region . Happy Path Chaining .

    [Test]
    public async Task happy_path_chain_with_then_map_and_on_success() {
        // Arrange
        var         initialValue = new GameId(Faker.Random.Guid());
        var         task         = ValueTask.FromResult(initialValue);
        GameUpdated mappedValue  = null!;

        // Act
        var finalResult = await task
            .ToResultAsync(ex => new InvalidMoveError(initialValue.Value, ex.Message))
            .ThenAsync(gameId => Kurrent.Result.Success<GameUpdated, InvalidMoveError>(new GameUpdated(gameId.Value, GameStatus.Ongoing)))
            .MapAsync(gameUpdated => {
                mappedValue = gameUpdated;
                return new PlayerTurn(Player.X, new Position(1, 1), $"Turn for {gameUpdated.GameId}");
            })
            .OnSuccessAsync(playerTurn => {
                    playerTurn.Player.ShouldBe(Player.X);
                    return ValueTask.CompletedTask;
                }
            );

        // Assert
        finalResult.IsSuccess.ShouldBeTrue();
        finalResult.Value.MoveDescription!.ShouldContain(initialValue.Value.ToString());
        mappedValue.ShouldNotBeNull();
    }

    #endregion

    class GameState {
        public GameStatus Status { get; set; }
    }

    #region . Error Propagation Chaining .

    [Test]
    public async Task chain_with_early_error_short_circuits() {
        // Arrange
        var initialError = new InvalidMoveError(Faker.Random.Guid(), "Initial task failed");
        var task         = ValueTask.FromException<GameId>(new InvalidOperationException(initialError.Reason));
        var thenExecuted = false;

        // Act
        var finalResult = await task
            .ToResultAsync(ex => new InvalidMoveError(initialError.GameId, ex.Message))
            .ThenAsync(gameId => {
                thenExecuted = true;
                return Kurrent.Result.Success<GameUpdated, InvalidMoveError>(new GameUpdated(gameId.Value, GameStatus.Ongoing));
            });

        // Assert
        finalResult.IsFailure.ShouldBeTrue();
        finalResult.Error.Reason.ShouldBe(initialError.Reason);
        thenExecuted.ShouldBeFalse();
    }

    [Test]
    public async Task chain_with_error_in_the_middle_short_circuits() {
        // Arrange
        var initialValue = new GameId(Faker.Random.Guid());
        var task         = ValueTask.FromResult(initialValue);
        var middleError  = new InvalidMoveError(initialValue.Value, "Middle operation failed");
        var mapExecuted  = false;

        // Act
        var finalResult = await task
            .ToResultAsync(ex => new InvalidMoveError(initialValue.Value, ex.Message))
            .ThenAsync(gameId => Kurrent.Result.Failure<GameUpdated, InvalidMoveError>(middleError))
            .MapAsync(gameUpdated => {
                mapExecuted = true;
                return new PlayerTurn(Player.X, new Position(1, 1));
            });

        // Assert
        finalResult.IsFailure.ShouldBeTrue();
        finalResult.Error.ShouldBe(middleError);
        mapExecuted.ShouldBeFalse();
    }

    #endregion

    #region . Unit and State Chaining .

    [Test]
    public async Task chain_with_mixed_unit_and_value_operations() {
        // Arrange
        var initialValue = new GameId(Faker.Random.Guid());
        var task         = ValueTask.FromResult(initialValue);

        // Act
        var finalResult = await task
            .ToResultAsync(ex => new InvalidMoveError(initialValue.Value, ex.Message))
            .ThenAsync(gameId => {
                    // Operation that returns a Unit result
                    return Kurrent.Result.Success<Void, InvalidMoveError>(Void.Value);
                }
            )
            .ThenAsync(unit => {
                    // Operation that takes a Unit and returns a value result
                    return Kurrent.Result.Success<PlayerTurn, InvalidMoveError>(new PlayerTurn(Player.O, new Position(2, 2)));
                }
            );

        // Assert
        finalResult.IsSuccess.ShouldBeTrue();
        finalResult.Value.Player.ShouldBe(Player.O);
    }

    [Test]
    public async Task chain_with_state_passing() {
        // Arrange
        var initialValue = new GameId(Faker.Random.Guid());
        var task         = ValueTask.FromResult(initialValue);
        var state        = new GameState { Status = GameStatus.Ongoing };

        // Act
        var finalResult = await task
            .ToResultAsync(ex => new InvalidMoveError(initialValue.Value, ex.Message))
            .ThenAsync(
                (gameId, s) => {
                    s.Status = GameStatus.Ongoing;
                    return Kurrent.Result.Success<GameUpdated, InvalidMoveError>(new GameUpdated(gameId.Value, s.Status));
                }, state
            )
            .MapAsync(
                (gameUpdated, s) => {
                    s.Status = GameStatus.Ongoing;
                    return new PlayerTurn(Player.X, new Position(1, 1), $"Game finished with status {s.Status}");
                }, state
            );

        // Assert
        finalResult.IsSuccess.ShouldBeTrue();
        finalResult.Value.MoveDescription.ShouldBe("Game finished with status Ongoing");
        state.Status.ShouldBe(GameStatus.Ongoing);
    }

    #endregion
}
