using KurrentDB.Client;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Kurrent.Client.Tests.Fast;

/// <summary>
/// Tests for the Try monad implementation.
/// </summary>
public class TryTests {
    #region Construction and Conversion

    [Test]
    public void returns_success_result_when_created_with_value() {
        // Arrange
        var value = 42;

        // Act
        var result = new Try<int>(value);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.IsError.ShouldBeFalse();
        result.SuccessValue().ShouldBe(value);
    }

    [Test]
    public void returns_error_result_when_created_with_exception() {
        // Arrange
        var exception = new InvalidOperationException("Test exception");

        // Act
        var result = new Try<int>(exception);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.IsError.ShouldBeTrue();
        result.ErrorValue().ShouldBe(exception);
    }

    [Test]
    public void converts_success_value_to_try_implicitly() {
        // Arrange & Act
        Try<int> result = 42;

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.SuccessValue().ShouldBe(42);
    }

    [Test]
    public void converts_exception_to_try_implicitly() {
        // Arrange
        var exception = new InvalidOperationException("Test exception");

        // Act
        Try<int> result = exception;

        // Assert
        result.IsError.ShouldBeTrue();
        result.ErrorValue().ShouldBe(exception);
    }

    [Test]
    public void converts_to_base_result_type_when_requested() {
        // Arrange
        Try<int> tryResult = 42;

        // Act
        var result = tryResult.ToResult();

        // Assert
        result.ShouldBeOfType<Result<Exception, int>>();
        result.IsSuccess.ShouldBeTrue();
        result.SuccessValue().ShouldBe(42);
    }

    #endregion

    #region JustTry Static Methods

    [Test]
    public void returns_success_when_no_exception_thrown() {
        // Arrange & Act
        var result = JustTry.Catching(() => 42);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.SuccessValue().ShouldBe(42);
    }

    [Test]
    public void returns_error_when_exception_thrown() {
        // Arrange & Act
        var result = JustTry.Catching<int>(() => throw new InvalidOperationException("Test exception"));

        // Assert
        result.IsError.ShouldBeTrue();
        result.ErrorValue().ShouldBeOfType<InvalidOperationException>();
        result.ErrorValue().Message.ShouldBe("Test exception");
    }

    [Test]
    public async Task returns_success_when_no_exception_thrown_in_async_operation() {
        // Arrange & Act
        var result = await JustTry.CatchingAsync(() => Task.FromResult(42));

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.SuccessValue().ShouldBe(42);
    }

    [Test]
    public async Task returns_error_when_exception_thrown_in_async_operation() {
        // Arrange & Act
        var result = await JustTry.CatchingAsync<int>(() =>
            Task.FromException<int>(new InvalidOperationException("Test exception"))
        );

        // Assert
        result.IsError.ShouldBeTrue();
        result.ErrorValue().ShouldBeOfType<InvalidOperationException>();
        result.ErrorValue().Message.ShouldBe("Test exception");
    }

    [Test]
    public async Task returns_success_when_no_exception_thrown_in_value_async_operation() {
        // Arrange & Act
        var result = await JustTry.CatchAsync(() => new ValueTask<int>(42));

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.SuccessValue().ShouldBe(42);
    }

    [Test]
    public async Task returns_error_when_exception_thrown_in_value_async_operation() {
        // Arrange & Act
        var result = await JustTry.CatchAsync<int>(() =>
            ValueTask.FromException<int>(new InvalidOperationException("Test exception"))
        );

        // Assert
        result.IsError.ShouldBeTrue();
        result.ErrorValue().ShouldBeOfType<InvalidOperationException>();
        result.ErrorValue().Message.ShouldBe("Test exception");
    }

    #endregion

    #region Synchronous Operations

    [Test]
    public void transforms_success_value_when_mapping() {
        // Arrange
        Try<int> result = 42;

        // Act
        var mapped = result.Map(x => x.ToString());

        // Assert
        mapped.IsSuccess.ShouldBeTrue();
        mapped.SuccessValue().ShouldBe("42");
    }

    [Test]
    public void preserves_error_when_mapping() {
        // Arrange
        var      exception = new InvalidOperationException("Test exception");
        Try<int> result    = exception;

        // Act
        var mapped = result.Map(x => x.ToString());

        // Assert
        mapped.IsError.ShouldBeTrue();
        mapped.ErrorValue().ShouldBe(exception);
    }

    [Test]
    public void returns_error_when_exception_thrown_during_mapping() {
        // Arrange
        Try<int> result = 42;

        // Act
        var mapped = result.Map<string>(x => throw new InvalidOperationException("Mapping exception"));

        // Assert
        mapped.IsError.ShouldBeTrue();
        mapped.ErrorValue().ShouldBeOfType<InvalidOperationException>();
        mapped.ErrorValue().Message.ShouldBe("Mapping exception");
    }

    [Test]
    public void chains_operations_when_binding_success_result() {
        // Arrange
        Try<int> result = 42;

        // Act
        var bound = result.Then(x => new Try<string>(x.ToString()));

        // Assert
        bound.IsSuccess.ShouldBeTrue();
        bound.SuccessValue().ShouldBe("42");
    }

    [Test]
    public void preserves_error_when_binding() {
        // Arrange
        var      exception = new InvalidOperationException("Test exception");
        Try<int> result    = exception;

        // Act
        var bound = result.Then(x => new Try<string>(x.ToString()));

        // Assert
        bound.IsError.ShouldBeTrue();
        bound.ErrorValue().ShouldBe(exception);
    }

    [Test]
    public void performs_action_when_onsuccess_called_on_success_result() {
        // Arrange
        Try<int> result             = 42;
        var      wasActionPerformed = false;

        // Act
        var returnedTry = result.OnSuccess(x => wasActionPerformed = true);

        // Assert
        wasActionPerformed.ShouldBeTrue();
        returnedTry.ShouldBeSameAs(result);
    }

    [Test]
    public void skips_action_when_onsuccess_called_on_error_result() {
        // Arrange
        Try<int> result             = new InvalidOperationException("Test exception");
        var      wasActionPerformed = false;

        // Act
        var returnedTry = result.OnSuccess(x => wasActionPerformed = true);

        // Assert
        wasActionPerformed.ShouldBeFalse();
        returnedTry.ShouldBeSameAs(result);
    }

    [Test]
    public void performs_action_when_onerror_called_on_error_result() {
        // Arrange
        var       exception          = new InvalidOperationException("Test exception");
        Try<int>  result             = exception;
        Exception? capturedError    = null; // Nullable
        var       actionWasPerformed = false;


        // Act
        var returnedTry = result.OnError(ex => {
            capturedError = ex;
            actionWasPerformed = true;
        });

        // Assert
        actionWasPerformed.ShouldBeTrue();
        capturedError.ShouldBe(exception);
        returnedTry.ShouldBeSameAs(result);
    }

    [Test]
    public void skips_action_when_onerror_called_on_success_result() {
        // Arrange
        Try<int> result             = 42;
        var      wasActionPerformed = false;

        // Act
        var returnedTry = result.OnError(ex => wasActionPerformed = true);

        // Assert
        wasActionPerformed.ShouldBeFalse();
        returnedTry.ShouldBeSameAs(result);
    }

    [Test]
    public void combines_two_success_results_when_zipping() {
        // Arrange
        Try<int>    first  = 42;
        Try<string> second = "test";

        // Act
        var result = first.Zip(second, (i, s) => $"{i}-{s}");

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.SuccessValue().ShouldBe("42-test");
    }

    [Test]
    public void returns_first_error_when_first_result_is_error_during_zipping() {
        // Arrange
        var         exception = new InvalidOperationException("First error");
        Try<int>    first     = exception;
        Try<string> second    = "test";

        // Act
        var result = first.Zip(second, (i, s) => $"{i}-{s}");

        // Assert
        result.IsError.ShouldBeTrue();
        result.ErrorValue().ShouldBe(exception);
    }

    [Test]
    public void returns_second_error_when_second_result_is_error_during_zipping() {
        // Arrange
        Try<int>    first     = 42;
        var         exception = new InvalidOperationException("Second error");
        Try<string> second    = exception;

        // Act
        var result = first.Zip(second, (i, s) => $"{i}-{s}");

        // Assert
        result.IsError.ShouldBeTrue();
        result.ErrorValue().ShouldBe(exception);
    }

    [Test]
    public void zip_two_returns_error_when_combine_function_throws() {
        // Arrange
        Try<int>    first  = 42;
        Try<string> second = "test";

        // Act
        var result = first.Zip(second, (i, s) => {
            throw new InvalidOperationException("Combine error");
#pragma warning disable CS0162 // Unreachable code detected
            return $"{i}-{s}";
#pragma warning restore CS0162 // Unreachable code detected
        });

        // Assert
        result.IsError.ShouldBeTrue();
        result.ErrorValue().ShouldBeOfType<InvalidOperationException>();
        result.ErrorValue().Message.ShouldBe("Combine error");
    }

    #endregion

    #region FoldCatching Tests

    [Test]
    public void fold_catching_transforms_success_value_when_mapper_succeeds() {
        // Arrange
        Try<int> tryResult = 42;

        // Act
        var mappedResult = tryResult.FoldCatching(x => x.ToString());

        // Assert
        mappedResult.IsSuccess.ShouldBeTrue();
        mappedResult.SuccessValue().ShouldBe("42");
    }

    [Test]
    public void fold_catching_preserves_original_error_when_try_is_error() {
        // Arrange
        var      exception = new InvalidOperationException("Original error");
        Try<int> tryResult = exception;

        // Act
        var mappedResult = tryResult.FoldCatching(x => x.ToString());

        // Assert
        mappedResult.IsError.ShouldBeTrue();
        mappedResult.ErrorValue().ShouldBe(exception);
    }

    [Test]
    public void fold_catching_returns_new_error_when_mapper_throws() {
        // Arrange
        Try<int> tryResult = 42;
        var      mapException = new ArgumentException("Mapper failed");

        // Act
        var mappedResult = tryResult.FoldCatching<string>(x => throw mapException);

        // Assert
        mappedResult.IsError.ShouldBeTrue();
        mappedResult.ErrorValue().ShouldBe(mapException);
    }

    #endregion

    #region Then Exception Handling

    [Test]
    public void then_returns_error_try_when_binder_throws_exception() {
        // Arrange
        Try<int> result = 42;
        var      bindException = new NotSupportedException("Binder failed");

        // Act
        var boundResult = result.Then<string>(x => throw bindException);

        // Assert
        boundResult.IsError.ShouldBeTrue();
        boundResult.ErrorValue().ShouldBe(bindException);
    }

    #endregion

    #region Zip Overload Tests

    // Zip (Try, Try, Try)
    [Test]
    public void zip_three_combines_success_results() {
        Try<int>    t1 = 1;
        Try<string> t2 = "two";
        Try<bool>   t3 = true;
        var result = t1.Zip(t2, t3, (i, s, b) => $"{i}-{s}-{b}");
        result.IsSuccess.ShouldBeTrue();
        result.SuccessValue().ShouldBe("1-two-True");
    }

    [Test]
    public void zip_three_propagates_first_error() {
        var ex = new Exception("Error1");
        Try<int>    t1 = ex;
        Try<string> t2 = "two";
        Try<bool>   t3 = true;
        var result = t1.Zip(t2, t3, (i, s, b) => $"{i}-{s}-{b}");
        result.IsError.ShouldBeTrue();
        result.ErrorValue().ShouldBe(ex);
    }

    [Test]
    public void zip_three_propagates_second_error() {
        var ex = new Exception("Error2");
        Try<int>    t1 = 1;
        Try<string> t2 = ex;
        Try<bool>   t3 = true;
        var result = t1.Zip(t2, t3, (i, s, b) => $"{i}-{s}-{b}");
        result.IsError.ShouldBeTrue();
        result.ErrorValue().ShouldBe(ex);
    }

    [Test]
    public void zip_three_propagates_third_error() {
        var ex = new Exception("Error3");
        Try<int>    t1 = 1;
        Try<string> t2 = "two";
        Try<bool>   t3 = ex;
        var result = t1.Zip(t2, t3, (i, s, b) => $"{i}-{s}-{b}");
        result.IsError.ShouldBeTrue();
        result.ErrorValue().ShouldBe(ex);
    }

    [Test]
    public void zip_three_returns_error_when_combine_function_throws() {
        Try<int>    t1 = 1;
        Try<string> t2 = "two";
        Try<bool>   t3 = true;
        var combineException = new InvalidOperationException("Combine error for three");

        var result = t1.Zip(t2, t3, (i, s, b) => {
            throw combineException;
#pragma warning disable CS0162 // Unreachable code detected
            return $"{i}-{s}-{b}";
#pragma warning restore CS0162 // Unreachable code detected
        });

        result.IsError.ShouldBeTrue();
        result.ErrorValue().ShouldBe(combineException);
    }

    // Zip (Try, Try, Try, Try)
    [Test]
    public void zip_four_combines_success_results() {
        Try<int>    t1 = 1;
        Try<string> t2 = "two";
        Try<bool>   t3 = true;
        Try<double> t4 = 4.0;
        var result = t1.Zip(t2, t3, t4, (i, s, b, d) => $"{i}-{s}-{b}-{d}");
        result.IsSuccess.ShouldBeTrue();
        result.SuccessValue().ShouldBe("1-two-True-4");
    }

    [Test]
    public void zip_four_propagates_fourth_error() {
        var ex = new Exception("Error4");
        Try<int>    t1 = 1;
        Try<string> t2 = "two";
        Try<bool>   t3 = true;
        Try<double> t4 = ex;
        var result = t1.Zip(t2, t3, t4, (i, s, b, d) => $"{i}-{s}-{b}-{d}");
        result.IsError.ShouldBeTrue();
        result.ErrorValue().ShouldBe(ex);
    }

    [Test]
    public void zip_four_returns_error_when_combine_function_throws() {
        Try<int>    t1 = 1;
        Try<string> t2 = "two";
        Try<bool>   t3 = true;
        Try<double> t4 = 4.0;
        var combineException = new InvalidOperationException("Combine error for four");

        var result = t1.Zip(t2, t3, t4, (i, s, b, d) => {
            throw combineException;
#pragma warning disable CS0162 // Unreachable code detected
            return $"{i}-{s}-{b}-{d}";
#pragma warning restore CS0162 // Unreachable code detected
        });

        result.IsError.ShouldBeTrue();
        result.ErrorValue().ShouldBe(combineException);
    }


    // Zip (Try, Try, Try, Try, Try)
    [Test]
    public void zip_five_combines_success_results() {
        Try<int>    t1 = 1;
        Try<string> t2 = "two";
        Try<bool>   t3 = true;
        Try<double> t4 = 4.0;
        Try<char>   t5 = '5';
        var result = t1.Zip(t2, t3, t4, t5, (i, s, b, d, c) => $"{i}-{s}-{b}-{d}-{c}");
        result.IsSuccess.ShouldBeTrue();
        result.SuccessValue().ShouldBe("1-two-True-4-5");
    }

    [Test]
    public void zip_five_propagates_fifth_error() {
        var ex = new Exception("Error5");
        Try<int>    t1 = 1;
        Try<string> t2 = "two";
        Try<bool>   t3 = true;
        Try<double> t4 = 4.0;
        Try<char>   t5 = ex;
        var result = t1.Zip(t2, t3, t4, t5, (i, s, b, d, c) => $"{i}-{s}-{b}-{d}-{c}");
        result.IsError.ShouldBeTrue();
        result.ErrorValue().ShouldBe(ex);
    }

    [Test]
    public void zip_five_returns_error_when_combine_function_throws() {
        Try<int>    t1 = 1;
        Try<string> t2 = "two";
        Try<bool>   t3 = true;
        Try<double> t4 = 4.0;
        Try<char>   t5 = '5';
        var combineException = new InvalidOperationException("Combine error for five");

        var result = t1.Zip(t2, t3, t4, t5, (i, s, b, d, c) => {
            throw combineException;
#pragma warning disable CS0162 // Unreachable code detected
            return $"{i}-{s}-{b}-{d}-{c}";
#pragma warning restore CS0162 // Unreachable code detected
        });

        result.IsError.ShouldBeTrue();
        result.ErrorValue().ShouldBe(combineException);
    }

    #endregion

    #region JustTry Cancellation Tests

    [Test]
    public void throws_operation_canceled_exception_when_token_is_canceled_in_catching_async() {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        Should.Throw<OperationCanceledException>(async () =>
            await JustTry.CatchingAsync(() => Task.FromResult(42), cts.Token)
        );
    }

    [Test]
    public void throws_operation_canceled_exception_when_token_is_canceled_in_catching_value_async() {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        Should.Throw<OperationCanceledException>(async () =>
            await JustTry.CatchAsync(() => new ValueTask<int>(42), cts.Token)
        );
    }

    #endregion
}
