using Bogus;
using TicTacToe;

namespace Kurrent.Client.Tests.Internal;

public class ResultTests {
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
    public void returns_true_and_outputs_value_when_try_get_success_called_on_success_result() {
        // Arrange
        var successValue  = new GameId(Faker.Random.Guid());
        var successResult = Result<GameId, InvalidMoveError>.Success(successValue);

        // Act
        var retrieved = successResult.TryGetSuccess(out var outputValue);

        // Assert
        retrieved.ShouldBeTrue();
        outputValue.ShouldBe(successValue);
    }

    [Test]
    public void returns_false_and_default_when_try_get_success_called_on_error_result() {
        // Arrange
        var errorResult = Result<GameId, InvalidMoveError>.Error(new InvalidMoveError(Faker.Random.Guid(), Faker.Lorem.Sentence()));

        // Act
        var retrieved = errorResult.TryGetSuccess(out var outputValue);

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
    public void implicitly_converts_result_to_true_when_success() {
        // Arrange
        var successResult = Result<GameStarted, InvalidMoveError>.Success(new GameStarted(Faker.Random.Guid(), Faker.PickRandom<Player>()));

        // Act
        bool isSuccess = successResult;

        // Assert
        isSuccess.ShouldBeTrue();
    }

    [Test]
    public void implicitly_converts_result_to_false_when_error() {
        // Arrange
        var errorResult = Result<GameStarted, InvalidMoveError>.Error(new InvalidMoveError(Faker.Random.Guid(), "Test reason"));

        // Act
        bool isSuccess = errorResult;

        // Assert
        isSuccess.ShouldBeFalse();
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

    [Test]
    public void returns_success_debug_string_when_result_is_success() {
        // Arrange
        var successValue = new GameId(Guid.Parse("11223344-5566-7788-99AA-BBCCDDEEFF00"));
        var result       = Result<GameId, InvalidMoveError>.Success(successValue);

        // Act
        var debugString = result.ToDebugString();

        // Assert
        debugString.ShouldBe($"Success: {successValue.ToString()}");
    }

    [Test]
    public void returns_error_debug_string_when_result_is_error() {
        // Arrange
        var errorValue = new InvalidMoveError(Guid.Parse("AABBCCDD-EEFF-0011-2233-445566778899"), "Fixed Test Error");
        var result     = Result<GameId, InvalidMoveError>.Error(errorValue);

        // Act
        var debugString = result.ToDebugString();

        // Assert
        debugString.ShouldBe($"Error: {errorValue}");
    }

    [Test]
    public void returns_success_debug_string_with_null_value_when_success_value_is_null_for_reference_type() {
        // Arrange
        GameStarted? successValue = null;
        var          result       = Result<GameStarted?, InvalidMoveError>.Success(successValue);

        // Act
        var debugString = result.ToDebugString();

        // Assert
        debugString.ShouldBe("Success: null");
    }

    [Test]
    public void returns_error_debug_string_with_null_value_when_error_value_is_null_for_reference_type() {
        // Arrange
        InvalidMoveError? errorValue = null;
        var               result     = Result<GameStarted, InvalidMoveError?>.Error(errorValue);

        // Act
        var debugString = result.ToDebugString();

        // Assert
        debugString.ShouldBe("Error: null");
    }

    class TestResult<TSuccess, TError> : Result<TSuccess, TError> {
        public TestResult(bool isSuccess, TSuccess? success = default, TError? error = default)
            : base(isSuccess, success, error) { }
    }
}

public class ResultFunctionalTests {
    Faker Faker { get; } = new();

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
        var mappedResult = result.Map((gameId, notes) =>
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
        var mappedResult = result.Map((gameId, notes) =>
            new GameUpdated(gameId.Value, GameStatus.Draw, notes),
            stateNotes
        );

        // Assert
        mappedResult.IsError.ShouldBeTrue();
        mappedResult.AsError.ShouldBe(errorValue);
    }

    [Test]
    public void chains_operation_when_then_called_on_success_result_returning_success() {
        // Arrange
        var initialGameId = new GameId(Faker.Random.Guid());
        var result        = Result<GameId, InvalidMoveError>.Success(initialGameId);
        var playerForTurn = Faker.PickRandom<Player>();
        var positionForTurn = new Position(0,0);

        // Act
        var boundResult = result.Then(gameId =>
            Result<PlayerTurn, InvalidMoveError>.Success(
                new PlayerTurn(playerForTurn, positionForTurn, $"Move for game {gameId.Value}")
            )
        );

        // Assert
        boundResult.IsSuccess.ShouldBeTrue();
        boundResult.AsSuccess.Player.ShouldBe(playerForTurn);
        boundResult.AsSuccess.Position.ShouldBe(positionForTurn);
        boundResult.AsSuccess.MoveDescription.ShouldBe($"Move for game {initialGameId.Value}");
    }

    [Test]
    public void chains_operation_when_then_called_on_success_result_returning_error() {
        // Arrange
        var initialSuccess = new GameId(Faker.Random.Guid());
        var result         = Result<GameId, InvalidMoveError>.Success(initialSuccess);
        var nextError      = new InvalidMoveError(Faker.Random.Guid(), "Chained operation failed");

        // Act
        var boundResult = result.Then(gameId => Result<PlayerTurn, InvalidMoveError>.Error(nextError with { GameId = gameId.Value }));

        // Assert
        boundResult.IsError.ShouldBeTrue();
        boundResult.AsError.GameId.ShouldBe(initialSuccess.Value);
        boundResult.AsError.Reason.ShouldBe(nextError.Reason);
    }

    [Test]
    public void returns_original_error_when_then_called_on_error_result() {
        // Arrange
        var errorValue = new InvalidMoveError(Faker.Random.Guid(), Faker.Lorem.Sentence());
        var result     = Result<GameId, InvalidMoveError>.Error(errorValue);

        // Act
        var boundResult = result.Then(_ =>
            Result<PlayerTurn, InvalidMoveError>.Success(new PlayerTurn(Faker.PickRandom<Player>(), new Position(0, 0)))
        );

        // Assert
        boundResult.IsError.ShouldBeTrue();
        boundResult.AsError.ShouldBe(errorValue);
    }

    [Test]
    public void passes_state_to_binder_when_using_stateful_then_returning_success() {
        // Arrange
        var successValue = new GameId(Faker.Random.Guid());
        var result       = Result<GameId, InvalidMoveError>.Success(successValue);
        var statePlayer  = Faker.PickRandom<Player>();

        // Act
        var boundResult = result.Then(
            (gameId, player) =>
                Result<PlayerTurn, InvalidMoveError>.Success(new PlayerTurn(player, new Position(0, 1), $"Move by {player} for game {gameId.Value}")),
            statePlayer
        );

        // Assert
        boundResult.IsSuccess.ShouldBeTrue();
        boundResult.AsSuccess.Player.ShouldBe(statePlayer);
        boundResult.AsSuccess.Position.ShouldBe(new Position(0,1));
        boundResult.AsSuccess.MoveDescription.ShouldBe($"Move by {statePlayer} for game {successValue.Value}");
    }

    [Test]
    public void passes_state_to_binder_when_using_stateful_then_returning_error() {
        // Arrange
        var successValue = new GameId(Faker.Random.Guid());
        var result       = Result<GameId, InvalidMoveError>.Success(successValue);
        var stateContext = "Critical operation context";

        // Act
        var boundResult = result.Then(
            (gameId, context) => Result<PlayerTurn, InvalidMoveError>.Error(
                new InvalidMoveError(gameId.Value, "Stateful chained op failed", context)
            ),
            stateContext
        );

        // Assert
        boundResult.IsError.ShouldBeTrue();
        boundResult.AsError.GameId.ShouldBe(successValue.Value);
        boundResult.AsError.Reason.ShouldBe("Stateful chained op failed");
        boundResult.AsError.StateContext.ShouldBe(stateContext);
    }

    [Test]
    public void returns_original_error_when_using_stateful_then_on_error_result() {
        // Arrange
        var errorValue  = new InvalidMoveError(Faker.Random.Guid(), Faker.Lorem.Sentence());
        var result      = Result<GameId, InvalidMoveError>.Error(errorValue);
        var statePlayer = Faker.PickRandom<Player>();

        // Act
        var boundResult = result.Then(
            (_, player) =>
                Result<PlayerTurn, InvalidMoveError>.Success(new PlayerTurn(player, new Position(1, 1))),
            statePlayer
        );

        // Assert
        boundResult.IsError.ShouldBeTrue();
        boundResult.AsError.ShouldBe(errorValue);
    }

    [Test]
    public void executes_success_function_when_matching_success_result() {
        // Arrange
        var successValue = new GameStarted(Faker.Random.Guid(), Faker.PickRandom<Player>());
        var result       = Result<GameStarted, InvalidMoveError>.Success(successValue);

        // Act
        var matchOutput = result.Match(
            gs => $"Started: {gs.GameId} by {gs.StartingPlayer}",
            err => $"Error: {err.Reason}"
        );

        // Assert
        matchOutput.ShouldBe($"Started: {successValue.GameId} by {successValue.StartingPlayer}");
    }

    [Test]
    public void executes_error_function_when_matching_error_result() {
        // Arrange
        var errorValue = new InvalidMoveError(Faker.Random.Guid(), Faker.Lorem.Sentence());
        var result     = Result<GameStarted, InvalidMoveError>.Error(errorValue);

        // Act
        var matchOutput = result.Match(
            gs => $"Started: {gs.GameId} by {gs.StartingPlayer}",
            err => $"Error: {err.Reason} for game {err.GameId}"
        );

        // Assert
        matchOutput.ShouldBe($"Error: {errorValue.Reason} for game {errorValue.GameId}");
    }

    [Test]
    public void passes_state_to_success_function_when_using_stateful_match_on_success() {
        // Arrange
        var successValue = new GameStarted(Faker.Random.Guid(), Faker.PickRandom<Player>());
        var result       = Result<GameStarted, InvalidMoveError>.Success(successValue);
        var additionalInfo = "High stakes game";

        // Act
        var matchOutput = result.Match(
            (gs, info) => $"Started: {gs.GameId} by {gs.StartingPlayer}. Info: {info}",
            (err, info) => $"Error: {err.Reason}. Info: {info}",
            additionalInfo
        );

        // Assert
        matchOutput.ShouldBe($"Started: {successValue.GameId} by {successValue.StartingPlayer}. Info: {additionalInfo}");
    }

    [Test]
    public void passes_state_to_error_function_when_using_stateful_match_on_error() {
        // Arrange
        var errorValue  = new InvalidMoveError(Faker.Random.Guid(), Faker.Lorem.Sentence());
        var result      = Result<GameStarted, InvalidMoveError>.Error(errorValue);
        var additionalInfo = "During critical phase";

        // Act
        var matchOutput = result.Match(
            (gs, info) => $"Started: {gs.GameId}. Info: {info}",
            (err, info) => $"Error: {err.Reason} for game {err.GameId}. Info: {info}",
            additionalInfo
        );

        // Assert
        matchOutput.ShouldBe($"Error: {errorValue.Reason} for game {errorValue.GameId}. Info: {additionalInfo}");
    }

    [Test]
    public void executes_success_action_when_switching_on_success_result() {
        // Arrange
        var successValue          = new GameStarted(Faker.Random.Guid(), Faker.PickRandom<Player>());
        var result                = Result<GameStarted, InvalidMoveError>.Success(successValue);
        var successActionExecuted = false;
        Guid? gameIdFromAction = null;

        // Act
        result.Switch(
            gs => {
                successActionExecuted = true;
                gameIdFromAction = gs.GameId;
            },
            _ => { /* error action not expected */ }
        );

        // Assert
        successActionExecuted.ShouldBeTrue();
        gameIdFromAction.ShouldBe(successValue.GameId);
    }

    [Test]
    public void executes_error_action_when_switching_on_error_result() {
        // Arrange
        var errorValue            = new InvalidMoveError(Faker.Random.Guid(), Faker.Lorem.Sentence());
        var result                = Result<GameStarted, InvalidMoveError>.Error(errorValue);
        var errorActionExecuted   = false;
        string? reasonFromAction = null;

        // Act
        result.Switch(
             _ => { /* success action not expected */ },
            err => {
                errorActionExecuted = true;
                reasonFromAction = err.Reason;
            }
        );

        // Assert
        errorActionExecuted.ShouldBeTrue();
        reasonFromAction.ShouldBe(errorValue.Reason);
    }

    [Test]
    public void passes_state_to_success_action_when_using_stateful_switch_on_success() {
        // Arrange
        var successValue         = new GameStarted(Faker.Random.Guid(), Faker.PickRandom<Player>());
        var result               = Result<GameStarted, InvalidMoveError>.Success(successValue);
        var notificationChannel  = "Email";
        string? processedNotification = null;

        // Act
        result.Switch(
            (gs, channel) => processedNotification = $"Notified {gs.StartingPlayer} via {channel}",
            (_, _) => { /* error action not expected */ },
            notificationChannel
        );

        // Assert
        processedNotification.ShouldBe($"Notified {successValue.StartingPlayer} via {notificationChannel}");
    }

    [Test]
    public void passes_state_to_error_action_when_using_stateful_switch_on_error() {
        // Arrange
        var errorValue           = new InvalidMoveError(Faker.Random.Guid(), Faker.Lorem.Sentence());
        var result               = Result<GameStarted, InvalidMoveError>.Error(errorValue);
        var escalationLevel      = "High";
        string? loggedError = null;

        // Act
        result.Switch(
            (_, _) => { /* success action not expected */ },
            (err, level) => loggedError = $"Logged error '{err.Reason}' with escalation: {level}",
            escalationLevel
        );

        // Assert
        loggedError.ShouldBe($"Logged error '{errorValue.Reason}' with escalation: {escalationLevel}");
    }

    [Test]
    public void executes_action_with_state_and_returns_same_instance_when_on_success_stateful_called_on_success_result() {
        // Arrange
        var successValue   = new GameStarted(Faker.Random.Guid(), Faker.PickRandom<Player>());
        var originalResult = Result<GameStarted, InvalidMoveError>.Success(successValue);
        var auditTrail     = "Audit: Game started by ";
        string? fullAuditMessage = null;

        // Act
        var returnedResult = originalResult.OnSuccess((gs, trail) => fullAuditMessage = trail + gs.StartingPlayer, auditTrail);

        // Assert
        fullAuditMessage.ShouldBe(auditTrail + successValue.StartingPlayer);
        returnedResult.ShouldBeSameAs(originalResult);
    }

    [Test]
    public void executes_action_with_state_and_returns_same_instance_when_on_error_stateful_called_on_error_result() {
        // Arrange
        var errorValue     = new InvalidMoveError(Faker.Random.Guid(), Faker.Lorem.Sentence());
        var originalResult = Result<GameStarted, InvalidMoveError>.Error(errorValue);
        var alertSystem    = "PagerDuty";
        string? alertSentTo = null;

        // Act
        var returnedResult = originalResult.OnError((err, system) => alertSentTo = $"Alert for '{err.Reason}' sent to {system}", alertSystem);

        // Assert
        alertSentTo.ShouldBe($"Alert for '{errorValue.Reason}' sent to {alertSystem}");
        returnedResult.ShouldBeSameAs(originalResult);
    }

    [Test]
    public void executes_action_and_returns_same_instance_when_on_success_called_on_success_result() {
        // Arrange
        var successValue   = new GameStarted(Faker.Random.Guid(), Faker.PickRandom<Player>());
        var originalResult = Result<GameStarted, InvalidMoveError>.Success(successValue);
        var actionExecuted = false;

        // Act
        var returnedResult = originalResult.OnSuccess(_ => actionExecuted = true);

        // Assert
        actionExecuted.ShouldBeTrue();
        returnedResult.ShouldBeSameAs(originalResult);
    }

    [Test]
    public void does_not_execute_action_and_returns_same_instance_when_on_success_called_on_error_result() {
        // Arrange
        var errorValue     = new InvalidMoveError(Faker.Random.Guid(), Faker.Lorem.Sentence());
        var originalResult = Result<GameStarted, InvalidMoveError>.Error(errorValue);
        var actionExecuted = false;

        // Act
        var returnedResult = originalResult.OnSuccess(_ => actionExecuted = true);

        // Assert
        actionExecuted.ShouldBeFalse();
        returnedResult.ShouldBeSameAs(originalResult);
    }

    [Test]
    public void executes_action_and_returns_same_instance_when_on_error_called_on_error_result() {
        // Arrange
        var errorValue     = new InvalidMoveError(Faker.Random.Guid(), Faker.Lorem.Sentence());
        var originalResult = Result<GameStarted, InvalidMoveError>.Error(errorValue);
        var actionExecuted = false;

        // Act
        var returnedResult = originalResult.OnError(_ => actionExecuted = true);

        // Assert
        actionExecuted.ShouldBeTrue();
        returnedResult.ShouldBeSameAs(originalResult);
    }

    [Test]
    public void does_not_execute_action_and_returns_same_instance_when_on_error_called_on_success_result() {
        // Arrange
        var successValue   = new GameStarted(Faker.Random.Guid(), Faker.PickRandom<Player>());
        var originalResult = Result<GameStarted, InvalidMoveError>.Success(successValue);
        var actionExecuted = false;

        // Act
        var returnedResult = originalResult.OnError(_ => actionExecuted = true);

        // Assert
        actionExecuted.ShouldBeFalse();
        returnedResult.ShouldBeSameAs(originalResult);
    }

    [Test]
    public void returns_success_value_when_get_value_or_else_called_on_success() {
        // Arrange
        var successValue = new GameId(Faker.Random.Guid());
        var result       = Result<GameId, InvalidMoveError>.Success(successValue);

        // Act
        var value = result.GetValueOrElse(_ => new GameId(Faker.Random.Guid()));

        // Assert
        value.ShouldBe(successValue);
    }

    [Test]
    public void returns_fallback_value_when_get_value_or_else_called_on_error() {
        // Arrange
        var errorValue    = new InvalidMoveError(Faker.Random.Guid(), Faker.Lorem.Sentence());
        var result        = Result<GameId, InvalidMoveError>.Error(errorValue);
        var fallbackValue = new GameId(Faker.Random.Guid());

        // Act
        var value = result.GetValueOrElse(err => {
                err.ShouldBe(errorValue);
                return fallbackValue;
            }
        );

        // Assert
        value.ShouldBe(fallbackValue);
    }

    [Test]
    public void returns_success_value_when_get_value_or_else_stateful_called_on_success() {
        // Arrange
        var successValue = new GameId(Faker.Random.Guid());
        var result       = Result<GameId, InvalidMoveError>.Success(successValue);
        var stateStatus  = Faker.PickRandom<GameStatus>(); // State not used in success path for GetValueOrElse

        // Act
        var value = result.GetValueOrElse((_, _) => new GameId(Faker.Random.Guid()), stateStatus);

        // Assert
        value.ShouldBe(successValue);
    }
}

public readonly record struct GameId(Guid Value);

public record PlayerTurn(Player Player, Position Position, string? MoveDescription = null);

public record GameUpdated(Guid GameId, GameStatus NewStatus, string? UpdateNotes = null);

public record InvalidMoveError(Guid GameId, string Reason, string? StateContext = null);

public record GameEndedError(Guid GameId, GameStatus Status, string? FinalStateContext = null);
