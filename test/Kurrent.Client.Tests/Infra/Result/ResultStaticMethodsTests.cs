using Bogus;

namespace Kurrent.Client.Tests.Infra.Result;

public class ResultStaticMethodsTests {
    Faker Faker { get; } = new();

    #region . Sync .

    [Test]
    public void try_returns_success_when_function_succeeds() {
        // Arrange
        var expectedValue = new GameId(Faker.Random.Guid());
        var func          = () => expectedValue;

        // Act
        var result = Kurrent.Result.Try(func, ex => ex.Message);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(expectedValue);
    }

    [Test]
    public void try_returns_error_when_function_throws_exception() {
        // Arrange
        var          errorMessage = "Function failed";
        Func<GameId> func         = () => throw new InvalidOperationException(errorMessage);

        // Act
        var result = Kurrent.Result.Try(func, ex => ex.Message);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(errorMessage);
    }

    [Test]
    public void try_action_returns_success_when_action_succeeds() {
        // Arrange
        var action = () => { };

        // Act
        var result = Kurrent.Result.Try(action, ex => ex.Message);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(Void.Value);
    }

    [Test]
    public void try_action_returns_error_when_action_throws_exception() {
        // Arrange
        var    errorMessage = "Action failed";
        Action action       = () => throw new InvalidOperationException(errorMessage);

        // Act
        var result = Kurrent.Result.Try(action, ex => ex.Message);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(errorMessage);
    }

    #endregion

    #region . Async .

    [Test]
    public async Task try_async_returns_success_when_async_function_succeeds() {
        // Arrange
        var expectedValue = new GameId(Faker.Random.Guid());
        var func          = () => ValueTask.FromResult(expectedValue);

        // Act
        var result = await Kurrent.Result.TryAsync(func, ex => ex.Message);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(expectedValue);
    }

    [Test]
    public async Task try_async_returns_error_when_async_function_throws_exception() {
        // Arrange
        var errorMessage = "Async function failed";
        var func         = () => ValueTask.FromException<GameId>(new InvalidOperationException(errorMessage));

        // Act
        var result = await Kurrent.Result.TryAsync(func, ex => ex.Message);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(errorMessage);
    }

    [Test]
    public async Task try_action_async_returns_success_when_async_action_succeeds() {
        // Arrange
        var action = () => ValueTask.CompletedTask;

        // Act
        var result = await Kurrent.Result.TryAsync(action, ex => ex.Message);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(Void.Value);
    }

    [Test]
    public async Task try_action_async_returns_error_when_async_action_throws_exception() {
        // Arrange
        var errorMessage = "Async action failed";
        var action       = () => ValueTask.FromException(new InvalidOperationException(errorMessage));

        // Act
        var result = await Kurrent.Result.TryAsync(action, ex => ex.Message);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(errorMessage);
    }

    #endregion
}
