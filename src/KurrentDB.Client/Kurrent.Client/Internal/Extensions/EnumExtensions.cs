using System.Collections.Concurrent;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Kurrent.Client;

static class EnumExtensions {
    static readonly ConcurrentDictionary<Type, ConcurrentDictionary<Enum, string>> Cache = new();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Description<T>(this T value) where T : Enum {
        // Get or create the value dictionary for this enum type
        var valueCache = Cache.GetOrAdd(typeof(T), _ => new ConcurrentDictionary<Enum, string>());

        // Cast once to avoid multiple boxing operations
        Enum enumValue = value;

        return valueCache.GetOrAdd(
            enumValue, key => {
                var fieldInfo = typeof(T).GetField(key.ToString());

                return fieldInfo is null
                    ? key.ToString()
                    : fieldInfo.GetCustomAttribute<DescriptionAttribute>(false)?.Description ?? key.ToString();
            }
        );
    }
}
