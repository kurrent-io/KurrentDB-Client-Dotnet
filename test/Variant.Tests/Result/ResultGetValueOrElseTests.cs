using TicTacToe;

namespace Kurrent.Variant.Tests.Result;

public class ResultGetValueOrElseTests {
    Faker Faker { get; } = new();

    [Test]
    public void get_value_or_else_returns_success_value_when_called_on_success() {
        // Arrange
        var successValue = new GameId(Faker.Random.Guid());
        var result       = Kurrent.Result.Success<GameId, InvalidMoveError>(successValue);

        // Act
        var value = result.GetValueOrDefault(_ => new GameId(Faker.Random.Guid()));

        // Assert
        value.ShouldBe(successValue);
    }

    [Test]
    public void get_value_or_else_returns_fallback_value_when_called_on_error() {
        // Arrange
        var errorValue    = new InvalidMoveError(Faker.Random.Guid(), Faker.Lorem.Sentence());
        var result        = Kurrent.Result.Failure<GameId, InvalidMoveError>(errorValue);
        var fallbackValue = new GameId(Faker.Random.Guid());

        // Act
        var value = result.GetValueOrDefault(err => {
                err.ShouldBe(errorValue);
                return fallbackValue;
            }
        );

        // Assert
        value.ShouldBe(fallbackValue);
    }

    [Test]
    public void get_value_or_else_returns_success_value_when_stateful_variant_called_on_success() {
        // Arrange
        var successValue = new GameId(Faker.Random.Guid());
        var result       = Kurrent.Result.Success<GameId, InvalidMoveError>(successValue);
        var stateStatus  = Faker.PickRandom<GameStatus>(); // State not used in success path for GetValueOrElse

        // Act
        var value = result.GetValueOrDefault((_, _) => new GameId(Faker.Random.Guid()), stateStatus);

        // Assert
        value.ShouldBe(successValue);
    }

    [Test]
    public void get_value_or_else_passes_state_to_fallback_when_using_stateful_variant() {
        // Arrange
        var errorValue    = new InvalidMoveError(Faker.Random.Guid(), Faker.Lorem.Sentence());
        var result        = Kurrent.Result.Failure<GameId, InvalidMoveError>(errorValue);
        var fallbackValue = new GameId(Faker.Random.Guid());
        var contextState  = "Fallback context";

        // Act
        var value = result.GetValueOrDefault(
            (err, state) => {
                err.ShouldBe(errorValue);
                state.ShouldBe(contextState);
                return fallbackValue;
            },
            contextState
        );

        // Assert
        value.ShouldBe(fallbackValue);
    }

    [Test]
    public async Task get_value_or_else_async_returns_success_value_when_called_on_success() {
        // Arrange
        var successValue = new GameId(Faker.Random.Guid());
        var result = Kurrent.Result.Success<GameId, InvalidMoveError>(successValue);

        // Act
        var value = await result.GetValueOrDefaultAsync(_ => ValueTask.FromResult(new GameId(Faker.Random.Guid())));

        // Assert
        value.ShouldBe(successValue);
    }

    [Test]
    public async Task get_value_or_else_async_returns_fallback_value_when_called_on_error() {
        // Arrange
        var errorValue = new InvalidMoveError(Faker.Random.Guid(), Faker.Lorem.Sentence());
        var result = Kurrent.Result.Failure<GameId, InvalidMoveError>(errorValue);
        var fallbackValue = new GameId(Faker.Random.Guid());

        // Act
        var value = await result.GetValueOrDefaultAsync(err => {
                err.ShouldBe(errorValue);
                return ValueTask.FromResult(fallbackValue);
            }
        );

        // Assert
        value.ShouldBe(fallbackValue);
    }
}
