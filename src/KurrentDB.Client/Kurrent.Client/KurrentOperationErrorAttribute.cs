using System.Collections.Concurrent;
using System.Reflection;
using Google.Protobuf.Reflection;
using Kurrent.Client.Schema.Serialization.Protobuf;
using KurrentDB.Protocol;

namespace Kurrent.Client;

/// <summary>
/// Marks a readonly partial record struct to have error properties generated from protobuf annotations.
/// The source generator will automatically implement IResultError with compile-time resolved annotation data.
/// </summary>
[AttributeUsage(AttributeTargets.All)]
public sealed class KurrentOperationErrorAttribute : Attribute {
    /// <summary>
    /// Marks a readonly partial record struct to have error properties generated from protobuf annotations.
    /// The source generator will automatically implement IResultError with compile-time resolved annotation data.
    /// </summary>
    public KurrentOperationErrorAttribute(Type protobufMessageType) {
        MessageType       = protobufMessageType.EnsureTypeIsProtoMessage();
        MessageDescriptor = MessageType.GetProtoMessageDescriptor();
        Annotations       = KurrentOperationErrorAnnotations.GetAnnotations(MessageDescriptor);
    }

    /// <summary>
    /// Marks a readonly partial record struct to have error properties generated from protobuf annotations.
    /// The source generator will automatically implement IResultError with compile-time resolved annotation data.
    /// </summary>
    public KurrentOperationErrorAttribute(string typeName) : this(GetProtoMessageType(typeName)) { }

    static Type GetProtoMessageType(string typeName) {
        return Type.GetType(typeName)?.EnsureTypeIsProtoMessage()
            ?? throw new ArgumentException($"Type '{typeName}' not found.", nameof(typeName));
    }

    /// <summary>
    /// Gets the protobuf message type that contains the error annotations.
    /// </summary>
    public Type MessageType { get; }

    public MessageDescriptor MessageDescriptor { get; set; }

    public (string Code, string Message, ErrorSeverity Severity) Annotations { get; }

    public static KurrentOperationErrorAttribute? GetAttribute(Type type) =>
        type.GetCustomAttributes<KurrentOperationErrorAttribute>().FirstOrDefault();

    public static KurrentOperationErrorAttribute GetRequiredAttribute(Type type) =>
        GetAttribute(type) ?? throw new InvalidOperationException($"The type {type.Name} must have a KurrentOperationError attribute.");
}

static class KurrentOperationErrorAnnotations {
    static readonly ConcurrentDictionary<string, ErrorAnnotations> Annotations = new();

    const string ErrorAnnotationsNotFound = "ERROR_ANNOTATIONS_NOT_FOUND";

    static ErrorAnnotations Get(MessageDescriptor descriptor) =>
        Annotations.GetOrAdd(descriptor.FullName, static (_, descriptor) =>
            descriptor.GetRequiredMessageExtensionValue(CoreExtensions.ErrorInfo), descriptor);

    public static (string Code, string Message, ErrorSeverity Severity) GetAnnotations(MessageDescriptor descriptor) {
        try {
            var ann = Get(descriptor);
            return (ann.Code, ann.Message, (ErrorSeverity)ann.Severity);
        } catch {
            return (
                ErrorAnnotationsNotFound,
                $"Failed to retrieve error annotations for `{descriptor.FullName}`",
                ErrorSeverity.Fatal);
        }
    }

    public static (string Code, string Message, ErrorSeverity Severity) GetAnnotations(Type errorType) =>
        GetAnnotations(errorType.EnsureTypeIsProtoMessage().GetProtoMessageDescriptor());
}

public enum ErrorSeverity {
    /// <summary>
    /// The error is recoverable, the operation failed, but the session can continue.
    /// </summary>
    Recoverable = 0,

    /// <summary>
    /// The error is fatal and the session should be terminated.
    /// </summary>
    Fatal = 1,

    /// <summary>
    /// The error is retriable, the operation failed but can be retried.
    /// </summary>
    Retriable = 2,
}
