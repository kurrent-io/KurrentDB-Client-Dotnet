using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Google.Protobuf.WellKnownTypes;
using KurrentDB.Protocol.Projections.V1;
using static KurrentDB.Protocol.Projections.V1.Projections;
using Type = System.Type;

namespace KurrentDB.Client {
	public partial class KurrentDBProjectionManagementClient {
        static readonly JsonSerializerOptions DefaultJsonSerializerOptions = new JsonSerializerOptions();

		/// <summary>
		/// Gets the result of a projection as an untyped document.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="partition"></param>
		/// <param name="deadline"></param>
		/// <param name="userCredentials"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public async Task<JsonDocument> GetResultAsync(string name, string? partition = null,
			TimeSpan? deadline = null, UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default) {
			var value = await GetResultInternalAsync(name, partition, deadline, userCredentials, cancellationToken)
				.ConfigureAwait(false);

#if NET
			await using var stream = new MemoryStream();
#else
			using var stream = new MemoryStream();
#endif
			await using var writer = new Utf8JsonWriter(stream);
			var serializer = new ValueSerializer();
			serializer.Write(writer, value, DefaultJsonSerializerOptions);
			await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
			stream.Position = 0;

			return JsonDocument.Parse(stream);
		}

		/// <summary>
		/// Gets the result of a projection.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="partition"></param>
		/// <param name="serializerOptions"></param>
		/// <param name="deadline"></param>
		/// <param name="userCredentials"></param>
		/// <param name="cancellationToken"></param>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
#if !NETFRAMEWORK
		[RequiresUnreferencedCode("This methods requires Reflection support in JSON serialization. Use the overload that accepts JsonTypeInfo<T> instead.")]
		[RequiresDynamicCode("This methods requires Reflection support in JSON serialization. Use the overload that accepts JsonTypeInfo<T> instead.")]
#endif
		public async Task<T> GetResultAsync<T>(
			string name, string? partition = null,
			JsonSerializerOptions? serializerOptions = null,
			TimeSpan? deadline = null, UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default
		)
			=> JsonSerializer.Deserialize<T>(
				(await GetRawResultAsync(name, partition, deadline, userCredentials, cancellationToken).ConfigureAwait(false)).Span,
				serializerOptions
			)!;

		/// <summary>
		/// Gets the result of a projection.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="partition"></param>
		/// <param name="typeInfo"></param>
		/// <param name="deadline"></param>
		/// <param name="userCredentials"></param>
		/// <param name="cancellationToken"></param>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public async Task<T> GetResultAsync<T>(
			string name, string? partition,
			JsonTypeInfo<T> typeInfo,
			TimeSpan? deadline = null, UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default
		)
			=> JsonSerializer.Deserialize(
				(await GetRawResultAsync(name, partition, deadline, userCredentials, cancellationToken).ConfigureAwait(false)).Span,
				typeInfo
			)!;

		async Task<ReadOnlyMemory<byte>> GetRawResultAsync(
			string name, string? partition,
			TimeSpan? deadline, UserCredentials? userCredentials,
			CancellationToken cancellationToken
		) {
			var value = await GetResultInternalAsync(name, partition, deadline, userCredentials, cancellationToken)
				.ConfigureAwait(false);
#if NET
			await using var stream = new MemoryStream();
#else
			using var stream = new MemoryStream();
#endif
			await using var writer     = new Utf8JsonWriter(stream);
			var             serializer = new ValueSerializer();
			serializer.Write(writer, value, DefaultJsonSerializerOptions);
			await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
			stream.Position = 0;
			return stream.ToArray();
		}

		async ValueTask<Value> GetResultInternalAsync(string name, string? partition,
		                                                      TimeSpan? deadline, UserCredentials? userCredentials, CancellationToken cancellationToken) {
			var channelInfo = await GetChannelInfo(cancellationToken).ConfigureAwait(false);
			using var call = new ProjectionsClient(
				channelInfo.CallInvoker).ResultAsync(new ResultReq {
				Options = new ResultReq.Types.Options {
					Name = name,
					Partition = partition ?? string.Empty
				}
			}, KurrentDBCallOptions.CreateNonStreaming(Settings, deadline, userCredentials, cancellationToken));

			var response = await call.ResponseAsync.ConfigureAwait(false);
			return response.Result;
		}

		/// <summary>
		/// Gets the state of a projection as an untyped document.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="partition"></param>
		/// <param name="deadline"></param>
		/// <param name="userCredentials"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public async Task<JsonDocument> GetStateAsync(string name, string? partition = null,
			TimeSpan? deadline = null, UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default) {
			var value = await GetStateInternalAsync(name, partition, deadline, userCredentials, cancellationToken)
				.ConfigureAwait(false);

#if NET
			await using var stream = new MemoryStream();
#else
			using var stream = new MemoryStream();
#endif
			await using var writer = new Utf8JsonWriter(stream);
			var serializer = new ValueSerializer();
			serializer.Write(writer, value, DefaultJsonSerializerOptions);
			stream.Position = 0;
			await writer.FlushAsync(cancellationToken).ConfigureAwait(false);

			return JsonDocument.Parse(stream);
		}

		/// <summary>
		/// Gets the state of a projection.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="partition"></param>
		/// <param name="serializerOptions"></param>
		/// <param name="deadline"></param>
		/// <param name="userCredentials"></param>
		/// <param name="cancellationToken"></param>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
#if !NETFRAMEWORK
		[RequiresUnreferencedCode("This methods requires Reflection support in JSON serialization. Use the overload that accepts JsonTypeInfo<T> instead.")]
		[RequiresDynamicCode("This methods requires Reflection support in JSON serialization. Use the overload that accepts JsonTypeInfo<T> instead.")]
#endif
		public async Task<T> GetStateAsync<T>(
			string name, string? partition = null,
			JsonSerializerOptions? serializerOptions = null, TimeSpan? deadline = null,
			UserCredentials? userCredentials = null, CancellationToken cancellationToken = default
		)
			=> JsonSerializer.Deserialize<T>(
				(await GetRawStateAsync(name, partition, deadline, userCredentials, cancellationToken).ConfigureAwait(false)).Span,
				serializerOptions
			)!;

		/// <summary>
		/// Gets the state of a projection.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="partition"></param>
		/// <param name="typeInfo"></param>
		/// <param name="deadline"></param>
		/// <param name="userCredentials"></param>
		/// <param name="cancellationToken"></param>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public async Task<T> GetStateAsync<T>(
			string name, string? partition,
			JsonTypeInfo<T> typeInfo, TimeSpan? deadline = null,
			UserCredentials? userCredentials = null, CancellationToken cancellationToken = default
		)
			=> JsonSerializer.Deserialize(
				(await GetRawStateAsync(name, partition, deadline, userCredentials, cancellationToken).ConfigureAwait(false)).Span,
				typeInfo
			)!;

		async Task<ReadOnlyMemory<byte>> GetRawStateAsync(string name, string? partition ,
		                                                  TimeSpan? deadline,
		                                                  UserCredentials? userCredentials, CancellationToken cancellationToken) {
			var value = await GetStateInternalAsync(name, partition, deadline, userCredentials, cancellationToken)
				.ConfigureAwait(false);

#if NET
			await using var stream = new MemoryStream();
#else
			using var stream = new MemoryStream();
#endif
			await using var writer     = new Utf8JsonWriter(stream);
			var             serializer = new ValueSerializer();
			serializer.Write(writer, value, DefaultJsonSerializerOptions);
			await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
			stream.Position = 0;
			return stream.ToArray();
		}

		async ValueTask<Value> GetStateInternalAsync(string name, string? partition, TimeSpan? deadline,
		                                                     UserCredentials? userCredentials, CancellationToken cancellationToken) {
			var channelInfo = await GetChannelInfo(cancellationToken).ConfigureAwait(false);
			using var call = new ProjectionsClient(
				channelInfo.CallInvoker).StateAsync(new StateReq {
				Options = new StateReq.Types.Options {
					Name = name,
					Partition = partition ?? string.Empty
				}
			}, KurrentDBCallOptions.CreateNonStreaming(Settings, deadline, userCredentials, cancellationToken));

			var response = await call.ResponseAsync.ConfigureAwait(false);
			return response.State;
		}

		private class ValueSerializer : System.Text.Json.Serialization.JsonConverter<Value> {
			public override Value Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
				throw new NotSupportedException();

			public override void Write(Utf8JsonWriter writer, Value value, JsonSerializerOptions options) {
				switch (value.KindCase) {
					case Value.KindOneofCase.None:
						break;
					case Value.KindOneofCase.BoolValue:
						writer.WriteBooleanValue(value.BoolValue);
						break;
					case Value.KindOneofCase.NullValue:
						writer.WriteNullValue();
						break;
					case Value.KindOneofCase.NumberValue:
						writer.WriteNumberValue(value.NumberValue);
						break;
					case Value.KindOneofCase.StringValue:
						writer.WriteStringValue(value.StringValue);
						break;
					case Value.KindOneofCase.ListValue:
						writer.WriteStartArray();
						foreach (var item in value.ListValue.Values) {
							Write(writer, item, options);
						}

						writer.WriteEndArray();
						break;
					case Value.KindOneofCase.StructValue:
						writer.WriteStartObject();
						foreach (var map in value.StructValue.Fields) {
							writer.WritePropertyName(map.Key);
							Write(writer, map.Value, options);
						}

						writer.WriteEndObject();
						break;
				}
			}
		}
	}
}
