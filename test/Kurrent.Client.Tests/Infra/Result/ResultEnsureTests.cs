namespace Kurrent.Client.Tests.Infra.Result;

public class ResultEnsureTests {
    [Test]
    public void ensure_returns_success_when_predicate_is_true() {
        // Arrange
        var result = Result<int, string>.Success(10);

        // Act
        var ensuredResult = result.Ensure(x => x > 5, _ => "Value is not greater than 5");

        // Assert
        ensuredResult.IsSuccess.ShouldBeTrue();
        ensuredResult.AsSuccess.ShouldBe(10);
    }

    [Test]
    public void ensure_returns_error_when_predicate_is_false() {
        // Arrange
        var result = Result<int, string>.Success(3);

        // Act
        var ensuredResult = result.Ensure(x => x > 5, x => $"Value {x} is not greater than 5");

        // Assert
        ensuredResult.IsError.ShouldBeTrue();
        ensuredResult.AsError.ShouldBe("Value 3 is not greater than 5");
    }

    [Test]
    public void ensure_propagates_error_when_result_is_already_an_error() {
        // Arrange
        var result = Result<int, string>.Error("Initial error");

        // Act
        var ensuredResult = result.Ensure(x => x > 5, _ => "This error should not be used");

        // Assert
        ensuredResult.IsError.ShouldBeTrue();
        ensuredResult.AsError.ShouldBe("Initial error");
    }

    [Test]
    public async Task ensure_async_returns_success_when_predicate_is_true() {
        // Arrange
        var result = Result<int, string>.Success(10);

        // Act
        var ensuredResult = await result.EnsureAsync(x => new ValueTask<bool>(x > 5), _ => "Value is not greater than 5");

        // Assert
        ensuredResult.IsSuccess.ShouldBeTrue();
        ensuredResult.AsSuccess.ShouldBe(10);
    }

    [Test]
    public async Task ensure_async_returns_error_when_predicate_is_false() {
        // Arrange
        var result = Result<int, string>.Success(3);

        // Act
        var ensuredResult = await result.EnsureAsync(x => new ValueTask<bool>(x > 5), x => $"Value {x} is not greater than 5");

        // Assert
        ensuredResult.IsError.ShouldBeTrue();
        ensuredResult.AsError.ShouldBe("Value 3 is not greater than 5");
    }

    [Test]
    public async Task ensure_async_propagates_error_when_result_is_already_an_error() {
        // Arrange
        var result = Result<int, string>.Error("Initial error");

        // Act
        var ensuredResult = await result.EnsureAsync(x => new ValueTask<bool>(x > 5), _ => "This error should not be used");

        // Assert
        ensuredResult.IsError.ShouldBeTrue();
        ensuredResult.AsError.ShouldBe("Initial error");
    }

    [Test]
    public void ensure_with_state_returns_success_when_predicate_is_true() {
        // Arrange
        var result = Result<int, string>.Success(10);
        var state = 5;

        // Act
        var ensuredResult = result.Ensure((x, s) => x > s, (x, s) => $"Value {x} is not greater than {s}", state);

        // Assert
        ensuredResult.IsSuccess.ShouldBeTrue();
        ensuredResult.AsSuccess.ShouldBe(10);
    }

    [Test]
    public void ensure_with_state_returns_error_when_predicate_is_false() {
        // Arrange
        var result = Result<int, string>.Success(3);
        var state = 5;

        // Act
        var ensuredResult = result.Ensure((x, s) => x > s, (x, s) => $"Value {x} is not greater than {s}", state);

        // Assert
        ensuredResult.IsError.ShouldBeTrue();
        ensuredResult.AsError.ShouldBe("Value 3 is not greater than 5");
    }

    [Test]
    public async Task ensure_async_with_state_returns_success_when_predicate_is_true() {
        // Arrange
        var result = Result<int, string>.Success(10);
        var state = 5;

        // Act
        var ensuredResult = await result.EnsureAsync((x, s) => new ValueTask<bool>(x > s), (x, s) => $"Value {x} is not greater than {s}", state);

        // Assert
        ensuredResult.IsSuccess.ShouldBeTrue();
        ensuredResult.AsSuccess.ShouldBe(10);
    }

    [Test]
    public async Task ensure_async_with_state_returns_error_when_predicate_is_false() {
        // Arrange
        var result = Result<int, string>.Success(3);
        var state = 5;

        // Act
        var ensuredResult = await result.EnsureAsync((x, s) => new ValueTask<bool>(x > s), (x, s) => $"Value {x} is not greater than {s}", state);

        // Assert
        ensuredResult.IsError.ShouldBeTrue();
        ensuredResult.AsError.ShouldBe("Value 3 is not greater than 5");
    }
}
