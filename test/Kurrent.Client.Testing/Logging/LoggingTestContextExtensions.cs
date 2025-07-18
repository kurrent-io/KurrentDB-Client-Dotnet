using System.Diagnostics.CodeAnalysis;
using Serilog;

namespace Kurrent.Client.Testing.Logging;

public static class LoggingTestContextExtensions {
    const string LoggerKey   = "$ToolkitLogger";

    public static void SetLogger(this TestContext context, ILogger logger) =>
	    context.ObjectBag[LoggerKey] = logger;

    public static bool TryGetLogger(this TestContext? context, [MaybeNullWhen(false)] out ILogger logger) {
        if (context is not null
         && context.ObjectBag.TryGetValue(LoggerKey, out var value)
         && value is ILogger serilogLogger) {
            logger = serilogLogger;
            return true;
        }

        logger = null!;
        return false;
    }

    public static ILogger Logger(this TestContext? context)
	    => context is not null
	    && context.ObjectBag.TryGetValue(LoggerKey, out var value)
	    && value is ILogger logger
		    ? logger
		    : throw new InvalidOperationException("Testing toolkit logger not found!");
}
