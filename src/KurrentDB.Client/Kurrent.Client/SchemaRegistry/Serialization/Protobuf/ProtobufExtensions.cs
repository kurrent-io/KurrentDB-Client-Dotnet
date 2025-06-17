using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
using Google.Protobuf;
using Google.Protobuf.Reflection;

namespace Kurrent.Client.SchemaRegistry.Serialization.Protobuf;

static class ProtobufExtensions {
    public static MessageParser GetProtoMessageParser(this Type messageType) =>
        ProtobufMessages.System.GetParser(messageType);

    public static MessageDescriptor GetProtoMessageDescriptor(this Type messageType) =>
        ProtobufMessages.System.GetDescriptor(messageType);

    public static Type EnsureTypeIsProtoMessage(this Type messageType) =>
	    !typeof(IMessage).IsAssignableFrom(messageType)
		    ? throw new InvalidCastException($"Type {messageType.Name} is not a Protocol Buffers message")
		    : messageType;

    public static bool IsProtoMessage(this Type messageType) =>
        typeof(IMessage).IsAssignableFrom(messageType);

    public static IMessage EnsureValueIsProtoMessage(this object? value) =>
	    value as IMessage ?? throw new InvalidOperationException($"Value of type {value!.GetType().Name} is not a Protocol Buffers message");


    public static bool TryGetMessageExtensionValue<TValue>(this MessageDescriptor descriptor, Extension<MessageOptions, TValue> extension, out TValue? value) where TValue : IMessage {
        var options = descriptor.GetOptions();

        if (options?.HasExtension(extension) ?? false) {
            value = options.GetExtension(extension);
            return true;
        }

        value = default!;
        return false;
    }

    public static TValue GetRequiredMessageExtensionValue<TValue>(this MessageDescriptor descriptor, Extension<MessageOptions, TValue> extension) where TValue : IMessage =>
        !descriptor.TryGetMessageExtensionValue(extension, out var value) || value is null ? throw new InvalidOperationException($"Type {descriptor.Name} does not have {typeof(TValue)} extension defined.") : value;

    public static bool TryGetFieldExtensionValue<TValue>(this MessageDescriptor descriptor, string fieldName, Extension<FieldOptions, TValue> extension, [MaybeNullWhen(false)] out TValue value) where TValue : IMessage {
        var options = descriptor.FindFieldByName(fieldName).GetOptions();
        if (options?.HasExtension(extension) ?? false) {
            value = options.GetExtension(extension);
            return true;
        }

        value = default!;
        return false;
    }

    public static ReadOnlyMemory<byte> ToUtf8JsonBytes(this IMessage message) {
	    return Encoding.UTF8.GetBytes(
		    RemoveWhitespacesExceptInQuotes(JsonFormatter.Default.Format(message))
	    );

	    // simply because protobuf is so stupid that it adds spaces
	    // between property names and values. absurd...
	    static string RemoveWhitespacesExceptInQuotes(string json) {
		    var inQuotes = false;

		    var result = new StringBuilder(json.Length);

		    foreach (var c in json) {
			    if (c == '\"') {
				    inQuotes = !inQuotes;
				    result.Append(c); // Always include the quote characters
			    } else if (inQuotes || (!inQuotes && !char.IsWhiteSpace(c)))
				    result.Append(c);
		    }

		    return result.ToString();
	    }
    }
}

class ProtobufMessages {
    public static ProtobufMessages System { get; } = new();

    ConcurrentDictionary<Type, (MessageParser Parser, MessageDescriptor Descriptor)> Types { get; } = new();

    public MessageParser GetParser(Type messageType) =>
        Types.GetOrAdd(messageType, GetContext).Parser;

    public MessageDescriptor GetDescriptor(Type messageType) =>
        Types.GetOrAdd(messageType, GetContext).Descriptor;

    public MessageDescriptor GetDescriptor<T>() =>
        GetDescriptor(typeof(T));

    static (MessageParser Parser, MessageDescriptor Descriptor) GetContext(Type messageType) {
        return (GetMessageParser(messageType), GetMessageDescriptor(messageType));

        static MessageParser GetMessageParser(Type messageType) =>
            (MessageParser)messageType
                .GetProperty("Parser", BindingFlags.Public | BindingFlags.Static)!
                .GetValue(null)!;

        static MessageDescriptor GetMessageDescriptor(Type messageType) =>
            (MessageDescriptor)messageType
                .GetProperty("Descriptor", BindingFlags.Public | BindingFlags.Static)!
                .GetValue(null)!;
    }

    static ConcurrentDictionary<Type, MessageOptions?> OptionsCache { get; } = new();

    public static bool TryGetMessageAnnotations<T, TAnnotations>(Extension<MessageOptions, TAnnotations> extension, [MaybeNullWhen(false)] out TAnnotations annotation) where T : IMessage where TAnnotations : IMessage {
        var options = OptionsCache.GetOrAdd(
            typeof(T), static type =>
                System.GetDescriptor(type).GetOptions());

        if (options?.HasExtension(extension) ?? false) {
            annotation = options.GetExtension(extension);
            return true;
        }

        annotation = default!;
        return false;
    }

    public void ClearCache() {
        Types.Clear();
        OptionsCache.Clear();
    }
}
