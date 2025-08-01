using System.Diagnostics;

namespace Kurrent.Client;

public static class WithExtension {
    [DebuggerStepThrough]
    public static T With<T>(this T instance, Action<T> update, bool when = true) {
        if (when)
            update(instance);

        return instance;
    }

    [DebuggerStepThrough]
    public static T With<T>(this T instance, Func<T, T> update, bool when = true) => when ? update(instance) : instance;
}
