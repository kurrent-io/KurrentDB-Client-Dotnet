using System.Collections.Concurrent;
using System.Reflection;
using System.Text;
using Google.Protobuf;
using Google.Protobuf.Reflection;

namespace KurrentDB.Client.Schema.Serialization.Protobuf;

public static class ProtobufExtensions {
    public static MessageParser GetProtoMessageParser(this Type messageType) =>
        ProtobufMessages.System.GetParser(messageType);

    public static MessageDescriptor GetProtoMessageDescriptor(this Type messageType) =>
        ProtobufMessages.System.GetDescriptor(messageType);

    public static Type EnsureTypeIsProtoMessage(this Type messageType) {
        if (!typeof(IMessage).IsAssignableFrom(messageType))
            throw new InvalidCastException($"Type {messageType.Name} is not a Protocol Buffers message");

        return messageType;
    }

    public static bool IsProtoMessage(this Type messageType) =>
        typeof(IMessage).IsAssignableFrom(messageType);

    public static IMessage EnsureValueIsProtoMessage(this object? value) {
        if (value is not IMessage message)
            throw new InvalidOperationException($"Value of type {value!.GetType().Name} is not a Protocol Buffers message");

        return message;
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
}
