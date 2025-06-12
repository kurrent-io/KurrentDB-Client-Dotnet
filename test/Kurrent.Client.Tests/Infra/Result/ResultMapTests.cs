using Bogus;
using TicTacToe;

namespace Kurrent.Client.Tests.Infra.Result;

public class ResultMapTests {
    Faker Faker { get; } = new();

    #region . Sync .

    [Test]
    public void transforms_success_value_when_mapping_success_result() {
        // Arrange
        var initialSuccess = new GameId(Faker.Random.Guid());
        var result         = Result<GameId, InvalidMoveError>.Success(initialSuccess);

        // Act
        var mappedResult = result.Map(gameId => new GameUpdated(gameId.Value, GameStatus.Draw));

        // Assert
        mappedResult.IsSuccess.ShouldBeTrue();
        mappedResult.AsSuccess.GameId.ShouldBe(initialSuccess.Value);
        mappedResult.AsSuccess.NewStatus.ShouldBe(GameStatus.Draw);
    }

    [Test]
    public void propagates_error_when_mapping_error_result() {
        // Arrange
        var errorValue = new InvalidMoveError(Faker.Random.Guid(), Faker.Lorem.Sentence());
        var result     = Result<GameId, InvalidMoveError>.Error(errorValue);

        // Act
        var mappedResult = result.Map(gameId => new GameUpdated(gameId.Value, GameStatus.Draw));

        // Assert
        mappedResult.IsError.ShouldBeTrue();
        mappedResult.AsError.ShouldBe(errorValue);
    }

    [Test]
    public void passes_state_to_mapper_when_using_stateful_map() {
        // Arrange
        var successValue = new GameId(Faker.Random.Guid());
        var result       = Result<GameId, InvalidMoveError>.Success(successValue);
        var stateNotes   = "Initial map";

        // Act
        var mappedResult = result.Map(
            (gameId, notes) =>
                new GameUpdated(gameId.Value, GameStatus.Draw, notes),
            stateNotes
        );

        // Assert
        mappedResult.IsSuccess.ShouldBeTrue();
        mappedResult.AsSuccess.GameId.ShouldBe(successValue.Value);
        mappedResult.AsSuccess.NewStatus.ShouldBe(GameStatus.Draw);
        mappedResult.AsSuccess.UpdateNotes.ShouldBe(stateNotes);
    }

    [Test]
    public void propagates_error_when_using_stateful_map_on_error_result() {
        // Arrange
        var errorValue = new InvalidMoveError(Faker.Random.Guid(), Faker.Lorem.Sentence());
        var result     = Result<GameId, InvalidMoveError>.Error(errorValue);
        var stateNotes = "Initial map";

        // Act
        var mappedResult = result.Map(
            (gameId, notes) =>
                new GameUpdated(gameId.Value, GameStatus.Draw, notes),
            stateNotes
        );

        // Assert
        mappedResult.IsError.ShouldBeTrue();
        mappedResult.AsError.ShouldBe(errorValue);
    }

    #endregion

    #region . Async .

    [Test]
    public async Task map_async_transforms_success_value_when_using_value_task_mapper() {
        // Arrange
        var initialSuccess = new GameId(Faker.Random.Guid());
        var result         = Result<GameId, InvalidMoveError>.Success(initialSuccess);

        // Act
        var mappedResult = await result.MapAsync(gameId => ValueTask.FromResult(new PlayerTurn(Player.O, new Position(2, 1), $"Turn for game {gameId.Value}")));

        // Assert
        mappedResult.IsSuccess.ShouldBeTrue();

        mappedResult.OnSuccess(x => {
                x.Player.ShouldBe(Player.O);
                x.Position.ShouldBe(new Position(2, 1));
                x.MoveDescription.ShouldContain(initialSuccess.Value.ToString());
            }
        );
    }

    [Test]
    public async Task map_async_propagates_error_when_mapping_error_result_with_value_task() {
        // Arrange
        var errorValue = new InvalidMoveError(Faker.Random.Guid(), "ValueTask mapping error");
        var result     = Result<GameId, InvalidMoveError>.Error(errorValue);

        // Act
        var mappedResult = await result.MapAsync(gameId => { return ValueTask.FromResult(new PlayerTurn(Player.X, new Position(0, 0))); });

        // Assert
        mappedResult.IsError.ShouldBeTrue();
        mappedResult.AsError.ShouldBe(errorValue);
    }

    [Test]
    public async Task map_async_passes_state_to_mapper_when_using_stateful_value_task_variant() {
        // Arrange
        var successValue = new GameId(Faker.Random.Guid());
        var result       = Result<GameId, InvalidMoveError>.Success(successValue);
        var playerState  = Player.X;

        // Act
        var mappedResult = await result.MapAsync((gameId, player) => ValueTask.FromResult(new PlayerTurn(player, new Position(1, 1), $"Stateful turn by {player}")), playerState);

        // Assert
        mappedResult.IsSuccess.ShouldBeTrue();
        mappedResult.AsSuccess.Player.ShouldBe(playerState);
        mappedResult.AsSuccess.Position.ShouldBe(new Position(1, 1));
        mappedResult.AsSuccess.MoveDescription.ShouldBe($"Stateful turn by {playerState}");
    }

    [Test]
    public async Task map_async_propagates_error_when_using_stateful_value_task_variant_on_error_result() {
        // Arrange
        var errorValue  = new InvalidMoveError(Faker.Random.Guid(), "Stateful ValueTask error");
        var result      = Result<GameId, InvalidMoveError>.Error(errorValue);
        var playerState = Player.O;

        // Act
        var mappedResult = await result.MapAsync((gameId, player) => ValueTask.FromResult(new PlayerTurn(player, new Position(2, 2))), playerState);

        // Assert
        mappedResult.IsError.ShouldBeTrue();
        mappedResult.AsError.ShouldBe(errorValue);
    }

    [Test]
    public async Task map_async_handles_async_operations_with_value_task() {
        // Arrange
        var initialValue = new GameId(Faker.Random.Guid());
        var result       = Result<GameId, InvalidMoveError>.Success(initialValue);

        // Act
        var mappedResult = await result.MapAsync(async gameId => {
                // Simulate async work
                await Task.Delay(1);
                return new PlayerTurn(Player.X, new Position(0, 0), $"Async turn for {gameId.Value}");
            }
        );

        // Assert
        mappedResult.IsSuccess.ShouldBeTrue();
        mappedResult.AsSuccess.MoveDescription!.ShouldContain(initialValue.Value.ToString());
    }

    [Test]
    public async Task map_async_preserves_original_error_details_when_propagating() {
        // Arrange
        var originalError = new InvalidMoveError(Faker.Random.Guid(), "Original detailed error", "Critical context");
        var result        = Result<GameId, InvalidMoveError>.Error(originalError);

        // Act
        var mappedResult = await result.MapAsync(async gameId => {
                await Task.Delay(1);
                return new GameUpdated(gameId.Value, GameStatus.Won, "Should not be reached");
            }
        );

        // Assert
        mappedResult.IsError.ShouldBeTrue();
        mappedResult.AsError.ShouldBe(originalError);
        mappedResult.AsError.GameId.ShouldBe(originalError.GameId);
        mappedResult.AsError.Reason.ShouldBe(originalError.Reason);
        mappedResult.AsError.StateContext.ShouldBe(originalError.StateContext);
    }

    #endregion
}
