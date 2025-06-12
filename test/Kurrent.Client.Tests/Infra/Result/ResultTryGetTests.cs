using Bogus;
using TicTacToe;

namespace Kurrent.Client.Tests.Infra.Result;

public class ResultTryGetTests {
    Faker Faker { get; } = new();

    [Test]
    public void returns_true_and_outputs_value_when_try_get_success_called_on_success_result() {
        // Arrange
        var successValue  = new GameId(Faker.Random.Guid());
        var successResult = Result<GameId, InvalidMoveError>.Success(successValue);

        // Act
        var retrieved = successResult.TryGetValue(out var outputValue);

        // Assert
        retrieved.ShouldBeTrue();
        outputValue.ShouldBe(successValue);
    }

    [Test]
    public void returns_false_and_default_when_try_get_success_called_on_error_result() {
        // Arrange
        var errorResult = Result<GameId, InvalidMoveError>.Error(new InvalidMoveError(Faker.Random.Guid(), Faker.Lorem.Sentence()));

        // Act
        var retrieved = errorResult.TryGetValue(out var outputValue);

        // Assert
        retrieved.ShouldBeFalse();
        outputValue.ShouldBe(default);
    }

    [Test]
    public void returns_true_and_outputs_value_when_try_get_error_called_on_error_result() {
        // Arrange
        var errorValue  = new GameEndedError(Faker.Random.Guid(), Faker.PickRandom<GameStatus>());
        var errorResult = Result<GameStarted, GameEndedError>.Error(errorValue);

        // Act
        var retrieved = errorResult.TryGetError(out var outputValue);

        // Assert
        retrieved.ShouldBeTrue();
        outputValue.ShouldBe(errorValue);
    }

    [Test]
    public void returns_false_and_default_when_try_get_error_called_on_success_result() {
        // Arrange
        var successResult = Result<GameStarted, GameEndedError>.Success(new GameStarted(Faker.Random.Guid(), Faker.PickRandom<Player>()));

        // Act
        var retrieved = successResult.TryGetError(out var outputValue);

        // Assert
        retrieved.ShouldBeFalse();
        outputValue.ShouldBe(default);
    }

    [Test]
    public void returns_true_and_outputs_complex_value_when_try_get_success_called_on_complex_success_result() {
        // Arrange
        var complexValue  = new PlayerTurn(Faker.PickRandom<Player>(), new Position(1, 2), "Complex move description");
        var successResult = Result<PlayerTurn, InvalidMoveError>.Success(complexValue);

        // Act
        var retrieved = successResult.TryGetValue(out var outputValue);

        // Assert
        retrieved.ShouldBeTrue();
        outputValue.ShouldBe(complexValue);
        outputValue!.Player.ShouldBe(complexValue.Player);
        outputValue.Position.ShouldBe(complexValue.Position);
        outputValue.MoveDescription.ShouldBe(complexValue.MoveDescription);
    }

    [Test]
    public void returns_true_and_outputs_complex_error_when_try_get_error_called_on_complex_error_result() {
        // Arrange
        var complexError = new InvalidMoveError(Faker.Random.Guid(), "Position already occupied", "Critical game state");
        var errorResult  = Result<PlayerTurn, InvalidMoveError>.Error(complexError);

        // Act
        var retrieved = errorResult.TryGetError(out var outputValue);

        // Assert
        retrieved.ShouldBeTrue();
        outputValue.ShouldBe(complexError);
        outputValue!.GameId.ShouldBe(complexError.GameId);
        outputValue.Reason.ShouldBe(complexError.Reason);
        outputValue.StateContext.ShouldBe(complexError.StateContext);
    }

    [Test]
    public void maintains_nullable_annotations_when_try_get_value_called_on_nullable_success_type() {
        // Arrange
        var nullableValue = Faker.Lorem.Word();
        var successResult = Result<string?, InvalidMoveError>.Success(nullableValue);

        // Act
        var retrieved = successResult.TryGetValue(out var outputValue);

        // Assert
        retrieved.ShouldBeTrue();
        outputValue.ShouldBe(nullableValue);
    }

    [Test]
    public void maintains_nullable_annotations_when_try_get_error_called_on_nullable_error_type() {
        // Arrange
        var nullableError = Faker.Lorem.Word();
        var errorResult   = Result<GameId, string?>.Error(nullableError);

        // Act
        var retrieved = errorResult.TryGetError(out var outputValue);

        // Assert
        retrieved.ShouldBeTrue();
        outputValue.ShouldBe(nullableError);
    }
}
