using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace KurrentDB.Client.Testing.Logging;

public static class LoggingTestContextExtensions {
    const string LoggerFactoryKey = "$ToolkitLoggerFactory";

    public static void SetLoggerFactory(this TestContext context, ILoggerFactory loggerFactory) =>
	    context.ObjectBag[LoggerFactoryKey] = loggerFactory;

    public static bool TryGetLoggerFactory(this TestContext? context, [MaybeNullWhen(false)] out IPartitionedLoggerFactory loggerFactory) {
        if (context is not null
         && context.ObjectBag.TryGetValue(LoggerFactoryKey, out var value)
         && value is IPartitionedLoggerFactory factory) {
            loggerFactory = factory;
            return true;
        }

        loggerFactory = null!;
        return false;
    }

    public static IPartitionedLoggerFactory LoggerFactory(this TestContext? context)
        => context is not null
        && context.ObjectBag.TryGetValue(LoggerFactoryKey, out var value)
        && value is IPartitionedLoggerFactory loggerFactory
            ? loggerFactory
            : throw new InvalidOperationException("Testing toolkit logger factory not found!");
}
