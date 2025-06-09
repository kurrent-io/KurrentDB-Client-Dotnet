using KurrentDB.Client;
using OneOf.Types;

namespace Kurrent.Client.Tests.Fast;

public class ResultTests {
    #region . Success .

    [Test]
    public void creates_success_result_with_value() {
        // Arrange
        const string successValue = "Success message";

        // Act
        var result = Result<string, string>.Success(successValue);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.IsError.ShouldBeFalse();
        result.SuccessValue().ShouldBe(successValue);
    }

    #endregion

    #region . Error .

    [Test]
    public void creates_error_result_with_value() {
        // Arrange
        const string errorValue = "Error message";

        // Act
        var result = Result<string, string>.Error(errorValue);

        // Assert
        result.IsError.ShouldBeTrue();
        result.IsSuccess.ShouldBeFalse();
        result.ErrorValue().ShouldBe(errorValue);
    }

    #endregion

    #region . SuccessValue .

    [Test]
    public void throws_when_accessing_success_value_on_error_result() {
        // Arrange
        var result = Result<string, int>.Error("Error");

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => result.SuccessValue());
    }

    #endregion

    #region . ErrorValue .

    [Test]
    public void throws_when_accessing_error_value_on_success_result() {
        // Arrange
        var result = Result<string, int>.Success(42);

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => result.ErrorValue());
    }

    #endregion

    #region . Implicit Conversions .

    [Test]
    public void implicitly_converts_success_value_to_result() {
        // Arrange
        const string successValue = "Success message";

        // Act
        Result<int, string> result = successValue;

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.SuccessValue().ShouldBe(successValue);
    }

    [Test]
    public void implicitly_converts_error_value_to_result() {
        // Arrange
        const string errorValue = "Error message";

        // Act
        Result<string, int> result = errorValue;

        // Assert
        result.IsError.ShouldBeTrue();
        result.ErrorValue().ShouldBe(errorValue);
    }

    [Test]
    public void implicitly_converts_success_wrapper_to_result() {
        // Arrange
        const string successValue = "Success message";

        var success = new Success<string>(successValue);

        // Act
        Result<string, string> result = success;

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.SuccessValue().ShouldBe(successValue);
    }

    [Test]
    public void implicitly_converts_error_wrapper_to_result() {
        // Arrange
        const string errorValue = "Error message";

        var error = new Error<string>(errorValue);

        // Act
        Result<string, int> result = error;

        // Assert
        result.IsError.ShouldBeTrue();
        result.ErrorValue().ShouldBe(errorValue);
    }

    #endregion

    #region . TryGet .

    [Test]
    public void try_get_success_returns_true_and_value_for_success_result() {
        // Arrange
        const int successValue = 42;

        var result = Result<string, int>.Success(successValue);

        // Act
        var success = result.TryGetSuccess(out var value);

        // Assert
        success.ShouldBeTrue();
        value.ShouldBe(successValue);
    }

    [Test]
    public void try_get_success_returns_false_and_default_for_error_result() {
        // Arrange
        var result = Result<string, int>.Error("Error");

        // Act
        var success = result.TryGetSuccess(out var value);

        // Assert
        success.ShouldBeFalse();
        value.ShouldBe(default);
    }

    [Test]
    public void try_get_error_returns_true_and_value_for_error_result() {
        // Arrange
        const string errorValue = "Error";

        var result = Result<string, int>.Error(errorValue);

        // Act
        var isError = result.TryGetError(out var value);

        // Assert
        isError.ShouldBeTrue();
        value.ShouldBe(errorValue);
    }

    [Test]
    public void try_get_error_returns_false_and_default_for_success_result() {
        // Arrange
        var result = Result<string, int>.Success(42);

        // Act
        var isError = result.TryGetError(out var value);

        // Assert
        isError.ShouldBeFalse();
        value.ShouldBeNull();
    }

    #endregion

    #region . Try .

    [Test]
    public void try_creates_success_result_when_func_succeeds() {
        // Arrange
        const string successValue = "Success";

        // Act
        var result = Result<string, string>.Try(() => successValue, ex => ex.Message);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.SuccessValue().ShouldBe(successValue);
    }

    [Test]
    public void try_creates_error_result_when_func_throws() {
        // Arrange
        var exception = new InvalidOperationException("Test exception");

        // Act
        var result = Result<string, string>.Try(() => throw exception, ex => ex.Message);

        // Assert
        result.IsError.ShouldBeTrue();
        result.ErrorValue().ShouldBe(exception.Message);
    }

    [Test]
    public void try_with_state_creates_success_result_when_func_succeeds() {
        // Arrange
        const string successValue = "Success";
        const string state        = "MyState";

        // Act
        var result = Result<string, string>.Try(
            (s) => {
                s.ShouldBe(state);
                return successValue;
            }, (s, ex) => {
                s.ShouldBe(state);
                return ex.Message;
            }, state
        );

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.SuccessValue().ShouldBe(successValue);
    }

    [Test]
    public void try_with_state_creates_error_result_when_func_throws() {
        // Arrange
        var exception = new InvalidOperationException("Test exception");

        const string state = "MyState";

        // Act
        var result = Result<string, string>.Try(
            (s) => {
                s.ShouldBe(state);
                throw exception;
            }, (s, ex) => {
                s.ShouldBe(state);
                return ex.Message;
            }, state
        );

        // Assert
        result.IsError.ShouldBeTrue();
        result.ErrorValue().ShouldBe(exception.Message);
    }

    #endregion

    #region . FromNullable .

    [Test]
    public void from_nullable_returns_success_for_non_null_reference_type() {
        // Arrange
        const string value = "Not null";

        // Act
        var result = Result<string, string>.FromNullable<string>(value, "Is null");

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.SuccessValue().ShouldBe(value);
    }

    [Test]
    public void from_nullable_returns_error_for_null_reference_type() {
        // Arrange
        const string error = "Is null";

        // Act
        var result = Result<string, string>.FromNullable<string>(null, error);

        // Assert
        result.IsError.ShouldBeTrue();
        result.ErrorValue().ShouldBe(error);
    }

    [Test]
    public void from_nullable_struct_returns_success_for_non_null_value_type() {
        // Arrange
        const int value = 5;

        // Act
        var result = Result<string, int>.FromNullableStruct<int>(value, "Is null");

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.SuccessValue().ShouldBe(value);
    }

    [Test]
    public void from_nullable_struct_returns_error_for_null_value_type() {
        // Arrange
        const string error = "Is null";

        // Act
        var result = Result<string, int>.FromNullableStruct<int>(null, error);

        // Assert
        result.IsError.ShouldBeTrue();
        result.ErrorValue().ShouldBe(error);
    }

    #endregion

    #region . Then .

    [Test]
    public void bind_transforms_success_result() {
        // Arrange
        var result = Result<string, int>.Success(42);

        // Act
        var transformed = result.Then(value => Result<string, string>.Success(value.ToString()));

        // Assert
        transformed.IsSuccess.ShouldBeTrue();
        transformed.SuccessValue().ShouldBe("42");
    }

    [Test]
    public void bind_preserves_error_result() {
        // Arrange
        const string errorMessage = "Error message";

        var result = Result<string, int>.Error(errorMessage);

        // Act
        var transformed = result.Then(_ => Result<string, string>.Success("Success"));

        // Assert
        transformed.IsError.ShouldBeTrue();
        transformed.ErrorValue().ShouldBe(errorMessage);
    }

    [Test]
    public void bind_with_state_transforms_success_result() {
        // Arrange
        var result = Result<string, int>.Success(42);

        const string state = "MyState";

        // Act
        var transformed = result.Then(
            (value, s) => {
                s.ShouldBe(state);
                return Result<string, string>.Success(value.ToString() + s);
            }, state
        );

        // Assert
        transformed.IsSuccess.ShouldBeTrue();
        transformed.SuccessValue().ShouldBe("42MyState");
    }

    [Test]
    public void bind_with_state_preserves_error_result() {
        // Arrange
        const string state        = "MyState";
        const string errorMessage = "Error message";

        var result = Result<string, int>.Error(errorMessage);

        // Act
        var transformed = result.Then((_, s) => Result<string, string>.Success($"Success{s}"), state);

        // Assert
        transformed.IsError.ShouldBeTrue();
        transformed.ErrorValue().ShouldBe(errorMessage);
    }

    #endregion

    #region . OrElse .

    [Test]
    public void bind_error_transforms_error_result() {
        // Arrange
        var result = Result<string, int>.Error("Error");

        // Act
        var transformed = result.OrElse(error => Result<int, int>.Error(error.Length));

        // Assert
        transformed.IsError.ShouldBeTrue();
        transformed.ErrorValue().ShouldBe(5); // Length of "Error"
    }

    [Test]
    public void bind_error_preserves_success_result() {
        // Arrange
        const int successValue = 42;
        var       result       = Result<string, int>.Success(successValue);

        // Act
        var transformed = result.OrElse(error => Result<int, int>.Error(error.Length));

        // Assert
        transformed.IsSuccess.ShouldBeTrue();
        transformed.SuccessValue().ShouldBe(successValue);
    }

    [Test]
    public void bind_error_with_state_transforms_error_result() {
        // Arrange
        var result = Result<string, int>.Error("Error");

        const string state = "MyState";

        // Act
        var transformed = result.OrElse(
            (error, s) => {
                s.ShouldBe(state);
                return Result<int, int>.Error(error.Length + s.Length);
            }, state
        );

        // Assert
        transformed.IsError.ShouldBeTrue();
        transformed.ErrorValue().ShouldBe(5 + 7); // Length of "Error" + "MyState"
    }

    [Test]
    public void bind_error_with_state_preserves_success_result() {
        // Arrange
        const string state        = "MyState";
        const int    successValue = 42;

        var result = Result<string, int>.Success(successValue);

        // Act
        var transformed = result.OrElse((error, s) => Result<int, int>.Error(error.Length + s.Length), state);

        // Assert
        transformed.IsSuccess.ShouldBeTrue();
        transformed.SuccessValue().ShouldBe(successValue);
    }

    #endregion

    #region . Map .

    [Test]
    public void map_transforms_success_value() {
        // Arrange
        var result = Result<string, int>.Success(42);

        // Act
        var transformed = result.Map(x => x.ToString());

        // Assert
        transformed.IsSuccess.ShouldBeTrue();
        transformed.SuccessValue().ShouldBe("42");
    }

    [Test]
    public void map_with_state_transforms_success_value() {
        // Arrange
        var          result = Result<string, int>.Success(42);
        const string state  = "MyState";

        // Act
        var transformed = result.Map(
            (x, s) => {
                s.ShouldBe(state);
                return x.ToString() + s;
            }, state
        );

        // Assert
        transformed.IsSuccess.ShouldBeTrue();
        transformed.SuccessValue().ShouldBe("42MyState");
    }

    [Test]
    public void map_preserves_error_result() {
        // Arrange
        const string errorMessage = "Error message";

        var result = Result<string, int>.Error(errorMessage);

        // Act
        var transformed = result.Map(x => x.ToString());

        // Assert
        transformed.IsError.ShouldBeTrue();
        transformed.ErrorValue().ShouldBe(errorMessage);
    }

    [Test]
    public void map_with_state_preserves_error_result() {
        // Arrange
        const string state        = "MyState";
        const string errorMessage = "Error message";

        var result = Result<string, int>.Error(errorMessage);

        // Act
        var transformed = result.Map((x, s) => x.ToString() + s, state);

        // Assert
        transformed.IsError.ShouldBeTrue();
        transformed.ErrorValue().ShouldBe(errorMessage);
    }

    #endregion

    #region . MapError .

    [Test]
    public void map_error_transforms_error_value() {
        // Arrange
        var result = Result<string, int>.Error("Error");

        // Act
        var transformed = result.MapError(error => error.Length);

        // Assert
        transformed.IsError.ShouldBeTrue();
        transformed.ErrorValue().ShouldBe(5); // Length of "Error"
    }

    [Test]
    public void map_error_with_state_transforms_error_value() {
        // Arrange
        const string state = "MyState";

        var result = Result<string, int>.Error("Error");

        // Act
        var transformed = result.MapError(
            (error, s) => {
                s.ShouldBe(state);
                return error.Length + s.Length;
            }, state
        );

        // Assert
        transformed.IsError.ShouldBeTrue();
        transformed.ErrorValue().ShouldBe(5 + 7); // Length of "Error" + "MyState"
    }

    [Test]
    public void map_error_preserves_success_result() {
        // Arrange
        const int successValue = 42;

        var result = Result<string, int>.Success(successValue);

        // Act
        var transformed = result.MapError(error => error.Length);

        // Assert
        transformed.IsSuccess.ShouldBeTrue();
        transformed.SuccessValue().ShouldBe(successValue);
    }

    [Test]
    public void map_error_with_state_preserves_success_result() {
        // Arrange
        const string state        = "MyState";
        const int    successValue = 42;

        var result = Result<string, int>.Success(successValue);

        // Act
        var transformed = result.MapError((error, s) => error.Length + s.Length, state);

        // Assert
        transformed.IsSuccess.ShouldBeTrue();
        transformed.SuccessValue().ShouldBe(successValue);
    }

    #endregion

    #region . OnSuccess .

    [Test]
    public void do_executes_action_on_success_result() {
        // Arrange
        var result          = Result<string, int>.Success(42);
        var wasActionCalled = false;

        // Act
        var returnedResult = result.OnSuccess(_ => wasActionCalled = true);

        // Assert
        wasActionCalled.ShouldBeTrue();
        returnedResult.ShouldBe(result);
    }

    [Test]
    public void do_with_state_executes_action_on_success_result() {
        // Arrange
        const string state = "MyState";

        var result          = Result<string, int>.Success(42);
        var wasActionCalled = false;

        string? capturedState = null;

        // Act
        var returnedResult = result.OnSuccess(
            (val, s) => {
                wasActionCalled = true;
                capturedState   = s;
            }, state
        );

        // Assert
        wasActionCalled.ShouldBeTrue();
        returnedResult.ShouldBe(result);
        capturedState.ShouldBe(state);
    }

    [Test]
    public void do_skips_action_on_error_result() {
        // Arrange
        var result          = Result<string, int>.Error("Error");
        var wasActionCalled = false;

        // Act
        var returnedResult = result.OnSuccess(_ => wasActionCalled = true);

        // Assert
        wasActionCalled.ShouldBeFalse();
        returnedResult.ShouldBe(result);
    }

    [Test]
    public void do_with_state_skips_action_on_error_result() {
        // Arrange
        const string state = "MyState";

        var result          = Result<string, int>.Error("Error");
        var wasActionCalled = false;

        // Act
        var returnedResult = result.OnSuccess((_, s) => wasActionCalled = true, state);

        // Assert
        wasActionCalled.ShouldBeFalse();
        returnedResult.ShouldBe(result);
    }

    #endregion

    #region . OnError .

    [Test]
    public void do_if_error_executes_action_on_error_result() {
        // Arrange
        var result          = Result<string, int>.Error("Error");
        var wasActionCalled = false;

        // Act
        var returnedResult = result.OnError(_ => wasActionCalled = true);

        // Assert
        wasActionCalled.ShouldBeTrue();
        returnedResult.ShouldBe(result);
    }

    [Test]
    public void do_if_error_with_state_executes_action_on_error_result() {
        // Arrange
        const string state = "MyState";

        var     result          = Result<string, int>.Error("Error");
        var     wasActionCalled = false;
        string? capturedState   = null;

        // Act
        var returnedResult = result.OnError(
            (err, s) => {
                wasActionCalled = true;
                capturedState   = s;
            }, state
        );

        // Assert
        wasActionCalled.ShouldBeTrue();
        returnedResult.ShouldBe(result);
        capturedState.ShouldBe(state);
    }

    [Test]
    public void do_if_error_skips_action_on_success_result() {
        // Arrange
        var result          = Result<string, int>.Success(42);
        var wasActionCalled = false;

        // Act
        var returnedResult = result.OnError(_ => wasActionCalled = true);

        // Assert
        wasActionCalled.ShouldBeFalse();
        returnedResult.ShouldBe(result);
    }

    [Test]
    public void do_if_error_with_state_skips_action_on_success_result() {
        // Arrange
        const string state = "MyState";

        var result          = Result<string, int>.Success(42);
        var wasActionCalled = false;

        // Act
        var returnedResult = result.OnError((_, s) => wasActionCalled = true, state);

        // Assert
        wasActionCalled.ShouldBeFalse();
        returnedResult.ShouldBe(result);
    }

    #endregion

    #region . GetValueOrElse .

    [Test]
    public void default_with_returns_success_value_for_success_result() {
        // Arrange
        const int successValue = 42;

        var result = Result<string, int>.Success(successValue);

        // Act
        var value = result.GetValueOrElse(error => error.Length);

        // Assert
        value.ShouldBe(successValue);
    }

    [Test]
    public void default_with_with_state_returns_success_value_for_success_result() {
        // Arrange
        const string state = "MyState";

        const int successValue = 42;
        var       result       = Result<string, int>.Success(successValue);

        // Act
        var value = result.GetValueOrElse((error, s) => error.Length + s.Length, state);

        // Assert
        value.ShouldBe(successValue);
    }

    [Test]
    public void default_with_returns_fallback_value_for_error_result() {
        // Arrange
        var result = Result<string, int>.Error("Error");

        // Act
        var value = result.GetValueOrElse(error => error.Length);

        // Assert
        value.ShouldBe(5); // Length of "Error"
    }

    [Test]
    public void default_with_with_state_returns_fallback_value_for_error_result() {
        // Arrange
        const string state = "MyState";

        var result = Result<string, int>.Error("Error");

        // Act
        var value = result.GetValueOrElse(
            (error, s) => {
                s.ShouldBe(state);
                return error.Length + s.Length;
            }, state
        );

        // Assert
        value.ShouldBe(5 + 7); // Length of "Error" + "MyState"
    }

    #endregion

    #region . Fold .

    [Test]
    public void fold_returns_transformed_success_value_for_success_result() {
        // Arrange
        var result = Result<string, int>.Success(42);

        // Act
        var transformed = result.Fold(
            error => $"Error: {error}",
            success => $"Success: {success}"
        );

        // Assert
        transformed.ShouldBe("Success: 42");
    }

    [Test]
    public void fold_with_state_returns_transformed_success_value_for_success_result() {
        // Arrange
        const string state = "MyState";

        var result = Result<string, int>.Success(42);

        // Act
        var transformed = result.Fold(
            (error, s) => $"Error: {error}{s}",
            (success, s) => $"Success: {success}{s}",
            state
        );

        // Assert
        transformed.ShouldBe("Success: 42MyState");
    }

    [Test]
    public void fold_returns_transformed_error_value_for_error_result() {
        // Arrange
        var result = Result<string, int>.Error("Failed");

        // Act
        var transformed = result.Fold(
            error => $"Error: {error}",
            success => $"Success: {success}"
        );

        // Assert
        transformed.ShouldBe("Error: Failed");
    }

    [Test]
    public void fold_with_state_returns_transformed_error_value_for_error_result() {
        // Arrange
        const string state = "MyState";

        var result = Result<string, int>.Error("Failed");

        // Act
        var transformed = result.Fold(
            (error, s) => $"Error: {error}{s}",
            (success, s) => $"Success: {success}{s}",
            state
        );

        // Assert
        transformed.ShouldBe("Error: FailedMyState");
    }

    #endregion

    #region . Zip .

    [Test]
    public void zip_combines_two_success_results() {
        // Arrange
        var firstResult  = Result<string, int>.Success(42);
        var secondResult = Result<string, string>.Success("value");

        // Act
        var combined = firstResult.Zip(secondResult, (a, b) => $"{a}:{b}");

        // Assert
        combined.IsSuccess.ShouldBeTrue();
        combined.SuccessValue().ShouldBe("42:value");
    }

    [Test]
    public void zip_returns_first_error_when_first_result_is_error() {
        // Arrange
        const string errorMessage = "First error";

        var firstResult  = Result<string, int>.Error(errorMessage);
        var secondResult = Result<string, string>.Success("value");

        // Act
        var combined = firstResult.Zip(secondResult, (a, b) => $"{a}:{b}");

        // Assert
        combined.IsError.ShouldBeTrue();
        combined.ErrorValue().ShouldBe(errorMessage);
    }

    [Test]
    public void zip_returns_second_error_when_second_result_is_error() {
        // Arrange
        const string errorMessage = "Second error";

        var firstResult  = Result<string, int>.Success(42);
        var secondResult = Result<string, string>.Error(errorMessage);

        // Act
        var combined = firstResult.Zip(secondResult, (a, b) => $"{a}:{b}");

        // Assert
        combined.IsError.ShouldBeTrue();
        combined.ErrorValue().ShouldBe(errorMessage);
    }

    [Test]
    public void zip_with_three_results_combines_all_success_results() {
        // Arrange
        var firstResult  = Result<string, int>.Success(1);
        var secondResult = Result<string, int>.Success(2);
        var thirdResult  = Result<string, int>.Success(3);

        // Act
        var combined = firstResult.Zip(
            secondResult,
            thirdResult,
            (a, b, c) => a + b + c
        );

        // Assert
        combined.IsSuccess.ShouldBeTrue();
        combined.SuccessValue().ShouldBe(6);
    }

    [Test]
    public void zip_with_four_results_combines_all_success_results() {
        // Arrange
        var firstResult  = Result<string, int>.Success(1);
        var secondResult = Result<string, int>.Success(2);
        var thirdResult  = Result<string, int>.Success(3);
        var fourthResult = Result<string, int>.Success(4);

        // Act
        var combined = firstResult.Zip(
            secondResult,
            thirdResult,
            fourthResult,
            (a, b, c, d) => a + b + c + d
        );

        // Assert
        combined.IsSuccess.ShouldBeTrue();
        combined.SuccessValue().ShouldBe(10);
    }

    [Test]
    public void zip_with_error_in_middle_returns_first_error_encountered() {
        // Arrange
        const string errorMessage = "Error in second";

        var firstResult  = Result<string, int>.Success(1);
        var secondResult = Result<string, int>.Error(errorMessage);
        var thirdResult  = Result<string, int>.Success(3);
        var fourthResult = Result<string, int>.Success(4);

        // Act
        var combined = firstResult.Zip(
            secondResult,
            thirdResult,
            fourthResult,
            (a, b, c, d) => a + b + c + d
        );

        // Assert
        combined.IsError.ShouldBeTrue();
        combined.ErrorValue().ShouldBe(errorMessage);
    }

    #endregion
}
