using System.Reflection;
using Google.Protobuf.Reflection;

namespace Kurrent.Client.Infra;

/// <summary>
/// Utility for reading protobuf error annotations from message types.
/// </summary>
internal static class ProtobufAnnotationReader {
    /// <summary>
    /// Reads error information from protobuf message type annotations.
    /// </summary>
    /// <param name="protobufMessageType">The protobuf message type to read annotations from.</param>
    /// <returns>Error information if found, null otherwise.</returns>
    public static ErrorAnnotationInfo? ReadErrorAnnotations(Type protobufMessageType) {
        try {
            var descriptor = GetMessageDescriptor(protobufMessageType);
            if (descriptor?.GetOptions()?.HasExtension(KurrentDB.Protocol.CoreExtensions.ErrorInfo) != true) {
                return null;
            }

            var annotations = descriptor.GetOptions().GetExtension(KurrentDB.Protocol.CoreExtensions.ErrorInfo);
            return new ErrorAnnotationInfo(
                Code: annotations.Code,
                Message: annotations.Message,
                IsFatal: 1 == (int)annotations.Severity
            );
        } catch {
            return null;
        }
    }

    /// <summary>
    /// Gets the message descriptor for a protobuf message type.
    /// </summary>
    /// <param name="messageType">The protobuf message type.</param>
    /// <returns>The message descriptor if found, null otherwise.</returns>
    public static MessageDescriptor? GetMessageDescriptor(Type messageType) {
        try {
            var descriptorProperty = messageType.GetProperty("Descriptor", BindingFlags.Public | BindingFlags.Static);
            return descriptorProperty?.GetValue(null) as MessageDescriptor;
        } catch {
            return null;
        }
    }

    /// <summary>
    /// Checks if a type has protobuf error annotations.
    /// </summary>
    /// <param name="protobufMessageType">The protobuf message type to check.</param>
    /// <returns>True if error annotations are present, false otherwise.</returns>
    public static bool HasErrorAnnotations(Type protobufMessageType) {
        try {
            var descriptor = GetMessageDescriptor(protobufMessageType);
            return descriptor?.GetOptions()?.HasExtension(KurrentDB.Protocol.CoreExtensions.ErrorInfo) == true;
        } catch {
            return false;
        }
    }
}

/// <summary>
/// Contains error annotation information from protobuf messages.
/// </summary>
/// <param name="Code">The error code.</param>
/// <param name="Message">The error message.</param>
/// <param name="IsFatal">Whether the error is fatal.</param>
internal record ErrorAnnotationInfo(string Code, string Message, bool IsFatal);