using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;

namespace Kurrent.Client;

static class Exceptions {
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static ExceptionDispatchInfo CaptureException(Exception exception) {
        ArgumentNullException.ThrowIfNull(exception);
        return ExceptionDispatchInfo.Capture(exception);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static T ThrowCaptured<T>(this ExceptionDispatchInfo exceptionInfo) {
        ArgumentNullException.ThrowIfNull(exceptionInfo);
        exceptionInfo.Throw();
        return default!; // This line will never execute
    }
}
