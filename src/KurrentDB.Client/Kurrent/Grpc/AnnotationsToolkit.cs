using System.Collections.Concurrent;
using System.Reflection;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using KurrentDB.Protocol;

namespace Kurrent.Grpc;

public static class AnnotationsToolkit {
    public enum ErrorSeverity {
        Recoverable = 0, Fatal = 1
    }

    static readonly ConcurrentDictionary<Type, ErrorInfo?>         _annotationCache = new();
    static readonly ConcurrentDictionary<Type, MessageDescriptor?> _descriptorCache = new();

    public static ErrorInfo? GetErrorAnnotations<T>() where T : IMessage<T> => GetErrorAnnotations(typeof(T));

    public static ErrorInfo? GetErrorAnnotations(Type messageType) {
        if (!typeof(IMessage).IsAssignableFrom(messageType))
            return null;

        return _annotationCache.GetOrAdd(
            messageType, type => {
                var descriptor = GetMessageDescriptor(type);
                if (descriptor == null)
                    return null;

                try {
                    // Try to get the error_info extension using the known extension
                    var options = descriptor.GetOptions();
                    if (options != null && options.HasExtension(CoreExtensions.ErrorInfo)) {
                        var annotations = options.GetExtension(CoreExtensions.ErrorInfo);
                        return new ErrorInfo(
                            annotations.Code,
                            (ErrorSeverity)(int)annotations.Severity,
                            annotations.HasMessage ? annotations.Message : null
                        );
                    }
                }
                catch {
                    // Ignore any reflection errors and return null
                }

                return null;
            }
        );
    }

    public static string? GetErrorCode<T>() where T : IMessage<T> => GetErrorCode(typeof(T));

    public static string? GetErrorCode(Type messageType) => GetErrorAnnotations(messageType)?.Code;

    public static ErrorSeverity? GetSeverity<T>() where T : IMessage<T> => GetSeverity(typeof(T));

    public static ErrorSeverity? GetSeverity(Type messageType) => GetErrorAnnotations(messageType)?.Severity;

    public static string? GetAnnotatedMessage<T>() where T : IMessage<T> => GetAnnotatedMessage(typeof(T));

    public static string? GetAnnotatedMessage(Type messageType) => GetErrorAnnotations(messageType)?.Message;

    public static bool IsRecoverable<T>() where T : IMessage<T> => IsRecoverable(typeof(T));

    public static bool IsRecoverable(Type messageType) => GetSeverity(messageType) == ErrorSeverity.Recoverable;

    public static bool IsFatal<T>() where T : IMessage<T> => IsFatal(typeof(T));

    public static bool IsFatal(Type messageType) => GetSeverity(messageType) == ErrorSeverity.Fatal;

    public static bool HasErrorAnnotations<T>() where T : IMessage<T> => HasErrorAnnotations(typeof(T));

    public static bool HasErrorAnnotations(Type messageType) => GetErrorAnnotations(messageType) != null;

    static MessageDescriptor? GetMessageDescriptor(Type messageType) {
        return _descriptorCache.GetOrAdd(
            messageType, type => {
                var descriptorProperty = type.GetProperty(
                    "Descriptor",
                    BindingFlags.Public | BindingFlags.Static
                );

                return descriptorProperty?.GetValue(null) as MessageDescriptor;
            }
        );
    }

    public static void ClearCache() {
        _annotationCache.Clear();
        _descriptorCache.Clear();
    }

    public sealed record ErrorInfo(
        string Code,
        ErrorSeverity Severity,
        string? Message = null
    );
}
