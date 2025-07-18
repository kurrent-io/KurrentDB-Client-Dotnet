namespace Kurrent.Variant.Tests;

// Now test our new IVariantResultError with actual IResultError types
public record SimpleConnectionError(string ErrorCode, string ErrorMessage) : IResultError {
    public Exception CreateException(Exception? innerException = null) =>
        new InvalidOperationException($"Connection Error: {ErrorMessage} (Code: {ErrorCode})", innerException);
}

public record SimpleTimeoutError(string ErrorCode, string ErrorMessage) : IResultError {
    public Exception CreateException(Exception? innerException = null) =>
        new TimeoutException($"Timeout Error: {ErrorMessage} (Code: {ErrorCode})", innerException);
}

public readonly partial record struct SimpleErrorVariant : IVariantResultError<SimpleConnectionError, SimpleTimeoutError> {
    // Should generate variant implementation with IResultError pass-through
}

public class VariantResultErrorTests {
    [Test]
    public void simple_error_variant_should_work_with_connection_error() {
        var connectionError = new SimpleConnectionError("CONN_001", "Connection failed");
        SimpleErrorVariant errorVariant = connectionError;

        errorVariant.IsSimpleConnectionError.ShouldBeTrue();
        errorVariant.IsSimpleTimeoutError.ShouldBeFalse();

        // Test IResultError passthrough - this verifies source generator is working
        errorVariant.ErrorCode.ShouldBe("CONN_001");
        errorVariant.ErrorMessage.ShouldBe("Connection failed");

        // Test exception creation
        var ex = errorVariant.CreateException();
        ex.ShouldNotBeNull();
        ex.Message.ShouldContain("Connection failed");
    }

    [Test]
    public void simple_error_variant_should_work_with_timeout_error() {
        var timeoutError = new SimpleTimeoutError("TIMEOUT_001", "Request timed out");
        SimpleErrorVariant errorVariant = timeoutError;

        errorVariant.IsSimpleTimeoutError.ShouldBeTrue();
        errorVariant.IsSimpleConnectionError.ShouldBeFalse();

        errorVariant.ErrorCode.ShouldBe("TIMEOUT_001");
        errorVariant.ErrorMessage.ShouldBe("Request timed out");

        // Test exception creation
        var ex = errorVariant.CreateException();
        ex.ShouldNotBeNull();
        ex.ShouldBeOfType<TimeoutException>();
        ex.Message.ShouldContain("Request timed out");
    }
}
