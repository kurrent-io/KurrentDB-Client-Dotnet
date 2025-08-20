using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;

namespace Kurrent.Client;

static class ExceptionExtensions {
    /// <summary>
    /// Captures the current exception and throws it, preserving the stack trace.
    /// Created to fool the compiler into not optimizing away the stack trace.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static T WithOriginalCallStack<T>(this T exception) where T : Exception {
        var captured = ExceptionDispatchInfo.Capture(exception);
        captured.Throw();
        return null!; // This line will never execute
    }
}
