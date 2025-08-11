using System.Text.Json;
using System.Text.Json.Nodes;
using KurrentDB.Client.Core.Internal;

namespace KurrentDB.Client.Schema.Serialization.Json;

/// <summary>
/// A serializer class that supports serialization and deserialization of objects
/// </summary>
public class JsonSerializer(JsonSerializerOptions? options = null) {
	JsonSerializerOptions Options { get; } = options ?? JsonSchemaSerializerOptions.DefaultJsonSerializerOptions;

	public ReadOnlyMemory<byte> Serialize(object? value) =>
		System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(value, Options);

	public object? Deserialize(ReadOnlyMemory<byte> data, Type type) =>
		!type.IsMissing()
			? System.Text.Json.JsonSerializer.Deserialize(data.Span, type, Options)
			: System.Text.Json.JsonSerializer.Deserialize<JsonNode>(data.Span, Options);

	public async ValueTask<object?> Deserialize(Stream data, Type type, CancellationToken cancellationToken = default) {
		var value = !type.IsMissing()
			? await System.Text.Json.JsonSerializer.DeserializeAsync(data, type, Options, cancellationToken).ConfigureAwait(false)
			: await System.Text.Json.JsonSerializer.DeserializeAsync<JsonNode>(data, Options, cancellationToken).ConfigureAwait(false);

		return value;
	}

	public T? Deserialize<T>(ReadOnlyMemory<byte> data) where T : class =>
		Deserialize(data, typeof(T)) as T;
}
