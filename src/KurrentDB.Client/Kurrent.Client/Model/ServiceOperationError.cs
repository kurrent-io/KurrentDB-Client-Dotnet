using System.Collections.Concurrent;
using System.Reflection;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Kurrent.Client.SchemaRegistry.Serialization.Protobuf;
using KurrentDB.Protocol;

namespace Kurrent.Client.Model;

public abstract record ServiceOperationError : IResultError {
    protected ServiceOperationError(Action<Metadata>? configure = null) {
        Metadata = new();
        configure?.Invoke(Metadata);

        var ann = ServiceOperationErrorAnnotations
            .GetAnnotations(GetType());

        ErrorCode    = ann.Code;
        ErrorMessage = ann.Message;
        IsFatal      = ann.IsFatal;
    }

    public string   ErrorCode    { get; }
    public string   ErrorMessage { get; }
    public bool     IsFatal      { get; }
    public Metadata Metadata     { get; }

    public Exception CreateException(Exception? innerException = null) =>
        new KurrentClientException(ErrorCode, ErrorMessage, Metadata, innerException);

    public override string ToString() => ErrorMessage;
}

static class ServiceOperationErrorAnnotations {
    static readonly ConcurrentDictionary<string, ErrorAnnotations> Annotations = new();

    const string ErrorAnnotationsNotFound = "ERROR_ANNOTATIONS_NOT_FOUND";

    static ErrorAnnotations Get(MessageDescriptor descriptor) =>
        Annotations.GetOrAdd(descriptor.FullName, static (_, descriptor) =>
            descriptor.GetRequiredMessageExtensionValue(CoreExtensions.ErrorInfo), descriptor);

    public static (string Code, string Message, bool IsFatal) GetAnnotations(MessageDescriptor descriptor) {
        try {
            var ann = Get(descriptor);
            return (ann.Code, ann.Message, ann.Severity.GetHashCode() == 1);
        } catch {
            return (
                ErrorAnnotationsNotFound,
                $"Failed to retrieve error annotations for `{descriptor.FullName}`",
                IsFatal: true);
        }
    }

    public static (string Code, string Message, bool IsFatal) GetAnnotations(Type errorType) =>
        GetAnnotations(errorType.EnsureTypeIsProtoMessage().GetProtoMessageDescriptor());
}

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
        Annotations       = ServiceOperationErrorAnnotations.GetAnnotations(MessageDescriptor);
    }

    /// <summary>
    /// Marks a readonly partial record struct to have error properties generated from protobuf annotations.
    /// The source generator will automatically implement IResultError with compile-time resolved annotation data.
    /// </summary>
    public KurrentOperationErrorAttribute(string typeName) : this(GetProtoMessageType(typeName)) { }

    static Type GetProtoMessageType(string typeName) =>
        Type.GetType(typeName)?.EnsureTypeIsProtoMessage()
     ?? throw new ArgumentException($"Type '{typeName}' not found.", nameof(typeName));

    /// <summary>
    /// Gets the protobuf message type that contains the error annotations.
    /// </summary>
    public Type MessageType { get; }

    public MessageDescriptor MessageDescriptor { get; set; }

    public (string Code, string Message, bool IsFatal) Annotations { get; }

    public static KurrentOperationErrorAttribute GetAttribute(Type type) =>
        type.GetCustomAttributes<KurrentOperationErrorAttribute>().FirstOrDefault()
     ?? throw new InvalidOperationException($"The type {type.Name} must have a KurrentOperationError attribute.");
}
