using Kurrent.Variant;

namespace Kurrent.Client;

static class VariantResultErrorExtensions {
    /// <summary>
    /// Dangerous method that casts the error values to the specified type.
    /// It is the caller's responsibility to ensure that the error types are available in the target type.
    /// If any of the error types is not correct, an <see cref="InvalidCastException"/> will be thrown.
    /// </summary>
    public static TError ForwardErrors<TError>(this IVariantResultError error) {
        try {
            return (TError)(dynamic)error.Value;
        }
        catch (Exception ex) {
            throw new InvalidCastException($"Cannot forward result error of type {error.Value.GetType().Name} to {typeof(TError).Name}.", ex);
        }
    }
}
