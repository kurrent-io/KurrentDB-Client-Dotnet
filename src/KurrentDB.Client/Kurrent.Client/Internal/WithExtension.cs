using System.Diagnostics;

namespace Kurrent.Client;

public static class WithExtension {
    [DebuggerStepThrough]
    public static T With<T>(this T instance, Action<T> apply) {
        apply(instance);
        return instance;
    }

    [DebuggerStepThrough]
    public static T With<T>(this T instance, Func<T, T> apply) {
        return apply(instance);
    }

    [DebuggerStepThrough]
    public static T With<T, TState>(this T instance, Action<T, TState> apply, TState state) {
        apply(instance, state);
        return instance;
    }

    [DebuggerStepThrough]
    public static T With<T, TState>(this T instance, Func<T, TState, T> apply, TState state) {
        return apply(instance, state);
    }
}
