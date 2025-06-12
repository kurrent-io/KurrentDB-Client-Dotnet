using Bogus;
using TicTacToe;

namespace Kurrent.Client.Tests.Infra.Result;

public class ResultCoreTests {
    Faker Faker { get; } = new();

    [Test]
    public void creates_success_result_when_using_success_factory_method() {
        // Arrange
        var gameId           = new GameId(Faker.Random.Guid());
        var gameStartedEvent = new GameStarted(Faker.Random.Guid(), Faker.PickRandom<Player>());

        // Act
        var resultGameId    = Result<GameId, InvalidMoveError>.Success(gameId);
        var resultTicTacToe = Result<GameStarted, InvalidMoveError>.Success(gameStartedEvent);

        // Assert
        resultGameId.IsSuccess.ShouldBeTrue();
        resultGameId.AsSuccess.ShouldBe(gameId);

        resultTicTacToe.IsSuccess.ShouldBeTrue();
        resultTicTacToe.AsSuccess.ShouldBe(gameStartedEvent);
    }

    [Test]
    public void creates_error_result_when_using_error_factory_method() {
        // Arrange
        var gameEndedError = new GameEndedError(Faker.Random.Guid(), Faker.PickRandom<GameStatus>());
        var ticTacToeError = new InvalidMoveError(Faker.Random.Guid(), "Invalid board position");

        // Act
        var resultGameEnded = Result<GameStarted, GameEndedError>.Error(gameEndedError);
        var resultTicTacToe = Result<GameStarted, InvalidMoveError>.Error(ticTacToeError);

        // Assert
        resultGameEnded.IsError.ShouldBeTrue();
        resultGameEnded.AsError.ShouldBe(gameEndedError);

        resultTicTacToe.IsError.ShouldBeTrue();
        resultTicTacToe.AsError.ShouldBe(ticTacToeError);
    }

    [Test]
    public void sets_is_success_true_when_constructed_with_success_flag_and_value() {
        // Arrange
        var successValue = new GameStarted(Faker.Random.Guid(), Faker.PickRandom<Player>());

        // Act
        var result = new TestResult<GameStarted, InvalidMoveError>(true, successValue);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.AsSuccess.ShouldBe(successValue);
    }

    [Test]
    public void sets_is_success_false_when_constructed_with_error_flag_and_value() {
        // Arrange
        var errorValue = new InvalidMoveError(Faker.Random.Guid(), "Board full");

        // Act
        var result = new TestResult<GameStarted, InvalidMoveError>(false, error: errorValue);

        // Assert
        result.IsError.ShouldBeTrue();
        result.AsError.ShouldBe(errorValue);
    }

    [Test]
    public void returns_true_for_is_success_when_result_contains_success_value() {
        // Arrange
        var successResult = Result<GameStarted, InvalidMoveError>.Success(new GameStarted(Faker.Random.Guid(), Faker.PickRandom<Player>()));

        // Act & Assert
        successResult.IsSuccess.ShouldBeTrue();
    }

    [Test]
    public void returns_false_for_is_success_when_result_contains_error_value() {
        // Arrange
        var errorResult = Result<GameStarted, InvalidMoveError>.Error(new InvalidMoveError(Faker.Random.Guid(), "Test reason"));

        // Act & Assert
        errorResult.IsSuccess.ShouldBeFalse();
    }

    [Test]
    public void returns_true_for_is_failure_when_result_contains_error_value() {
        // Arrange
        var errorResult = Result<GameStarted, InvalidMoveError>.Error(new InvalidMoveError(Faker.Random.Guid(), "Test reason"));

        // Act & Assert
        errorResult.IsError.ShouldBeTrue();
    }

    [Test]
    public void returns_false_for_is_failure_when_result_contains_success_value() {
        // Arrange
        var successResult = Result<GameStarted, InvalidMoveError>.Success(new GameStarted(Faker.Random.Guid(), Faker.PickRandom<Player>()));

        // Act & Assert
        successResult.IsError.ShouldBeFalse();
    }

    [Test]
    public void returns_success_value_when_accessing_as_success_on_success_result() {
        // Arrange
        var successValue  = new GameId(Faker.Random.Guid());
        var successResult = Result<GameId, InvalidMoveError>.Success(successValue);

        // Act & Assert
        successResult.AsSuccess.ShouldBe(successValue);
    }

    [Test]
    public void throws_invalid_operation_exception_when_accessing_as_success_on_error_result() {
        // Arrange
        var errorResult = Result<GameId, InvalidMoveError>.Error(new InvalidMoveError(Faker.Random.Guid(), Faker.Lorem.Sentence()));

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => errorResult.AsSuccess)
            .Message.ShouldBe("Result is not a success.");
    }

    [Test]
    public void returns_error_value_when_accessing_as_error_on_error_result() {
        // Arrange
        var errorValue  = new Position(Faker.Random.Int(0, 2), Faker.Random.Int(0, 2));
        var errorResult = Result<GameStarted, Position>.Error(errorValue);

        // Act & Assert
        errorResult.AsError.ShouldBe(errorValue);
    }

    [Test]
    public void throws_invalid_operation_exception_when_accessing_as_error_on_success_result() {
        // Arrange
        var successResult = Result<GameStarted, Position>.Success(new GameStarted(Faker.Random.Guid(), Faker.PickRandom<Player>()));

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => successResult.AsError)
            .Message.ShouldBe("Result is not an error.");
    }

    [Test]
    public void implicitly_converts_success_value_to_result() {
        // Arrange
        var successValue = new GameStarted(Faker.Random.Guid(), Faker.PickRandom<Player>());

        // Act
        Result<GameStarted, InvalidMoveError> result = successValue;

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.AsSuccess.ShouldBe(successValue);
    }

    [Test]
    public void implicitly_converts_error_value_to_result() {
        // Arrange
        var errorValue = new InvalidMoveError(Faker.Random.Guid(), "Cell occupied");

        // Act
        Result<GameStarted, InvalidMoveError> result = errorValue;

        // Assert
        result.IsError.ShouldBeTrue();
        result.AsError.ShouldBe(errorValue);
    }

    [Test]
    public void explicitly_converts_result_to_success_value_when_success() {
        // Arrange
        var successValue  = new GameId(Faker.Random.Guid());
        var successResult = Result<GameId, InvalidMoveError>.Success(successValue);

        // Act
        var extractedValue = (GameId)successResult;

        // Assert
        extractedValue.ShouldBe(successValue);
    }

    [Test]
    public void throws_invalid_operation_exception_when_explicitly_converting_error_result_to_success_value() {
        // Arrange
        var errorResult = Result<GameId, InvalidMoveError>.Error(new InvalidMoveError(Faker.Random.Guid(), Faker.Lorem.Sentence()));

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => (GameId)errorResult)
            .Message.ShouldBe("Result is not a success.");
    }

    [Test]
    public void explicitly_converts_result_to_error_value_when_error() {
        // Arrange
        var errorValue  = new InvalidMoveError(Faker.Random.Guid(), Faker.Lorem.Sentence());
        var errorResult = Result<GameStarted, InvalidMoveError>.Error(errorValue);

        // Act
        var extractedValue = (InvalidMoveError)errorResult;

        // Assert
        extractedValue.ShouldBe(errorValue);
    }

    [Test]
    public void throws_invalid_operation_exception_when_explicitly_converting_success_result_to_error_value() {
        // Arrange
        var successResult = Result<GameStarted, InvalidMoveError>.Success(new GameStarted(Faker.Random.Guid(), Faker.PickRandom<Player>()));

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => (InvalidMoveError)successResult)
            .Message.ShouldBe("Result is not an error.");
    }

    class TestResult<TSuccess, TError> : Result<TSuccess, TError> {
        public TestResult(bool isSuccess, TSuccess? success = default, TError? error = default)
            : base(isSuccess, success, error) { }
    }
}
