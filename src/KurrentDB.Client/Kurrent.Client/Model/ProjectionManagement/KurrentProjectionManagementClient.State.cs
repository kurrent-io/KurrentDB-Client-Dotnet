// ReSharper disable InconsistentNaming
// ReSharper disable SwitchStatementHandlesSomeKnownEnumValuesWithDefault

using System.Text.Json;
using EventStore.Client.Projections;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Kurrent.Client.Legacy;
using Kurrent.Client.Model;
using KurrentDB.Client;
using Type = System.Type;

namespace Kurrent.Client;

public partial class KurrentProjectionManagementClient {
	static readonly JsonSerializerOptions DefaultJsonSerializerOptions = new JsonSerializerOptions();

	/// <summary>
	/// Gets the result of a projection as an untyped document.
	/// </summary>
	/// <param name="name"></param>
	/// <param name="partition"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	public async ValueTask<Result<JsonDocument, GetResultError>> GetResult(
		string name, string? partition = null, CancellationToken cancellationToken = default
	) {
		try {
			var value = await GetResultInternal(name, partition, cancellationToken).ConfigureAwait(false);

			await using var stream = new MemoryStream();
			await using var writer = new Utf8JsonWriter(stream);

			var serializer = new ValueSerializer();
			serializer.Write(writer, value, DefaultJsonSerializerOptions);
			await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
			stream.Position = 0;

			return Result.Success<JsonDocument, GetResultError>(
				await JsonDocument
					.ParseAsync(stream, cancellationToken: cancellationToken)
					.ConfigureAwait(false)
			);
		} catch (Exception ex) when (ex.InnerException is RpcException rpcEx) {
			return Result.Failure<JsonDocument, GetResultError>(
				ex switch {
					AccessDeniedException     => rpcEx.AsAccessDeniedError(),
					NotAuthenticatedException => rpcEx.AsNotAuthenticatedError(),
					_                         => throw KurrentClientException.CreateUnknown(nameof(GetResult), ex)
				}
			);
		} catch (Exception ex) {
			throw KurrentClientException.CreateUnknown(nameof(GetResult), ex);
		}
	}

	/// <summary>
	/// Gets the result of a projection.
	/// </summary>
	/// <param name="name"></param>
	/// <param name="partition"></param>
	/// <param name="serializerOptions"></param>
	/// <param name="cancellationToken"></param>
	/// <typeparam name="T"></typeparam>
	/// <returns></returns>
	public async ValueTask<Result<T, GetResultError>> GetResult<T>(
		string name,
		string? partition = null,
		JsonSerializerOptions? serializerOptions = null,
		CancellationToken cancellationToken = default
	) where T : notnull {
		try {
			var value = await GetResultInternal(name, partition, cancellationToken)
				.ConfigureAwait(false);

			await using var stream = new MemoryStream();
			await using var writer = new Utf8JsonWriter(stream);

			var serializer = new ValueSerializer();
			serializer.Write(writer, value, DefaultJsonSerializerOptions);
			await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
			stream.Position = 0;

			return Result.Success<T, GetResultError>(JsonSerializer.Deserialize<T>(stream.ToArray(), serializerOptions)!);
		} catch (Exception ex) when (ex.InnerException is RpcException rpcEx) {
			return Result.Failure<T, GetResultError>(
				ex switch {
					AccessDeniedException     => rpcEx.AsAccessDeniedError(),
					NotAuthenticatedException => rpcEx.AsNotAuthenticatedError(),
					_                         => throw KurrentClientException.CreateUnknown(nameof(GetResult), ex)
				}
			);
		} catch (Exception ex) {
			throw KurrentClientException.CreateUnknown(nameof(GetResult), ex);
		}
	}

	/// <summary>
	/// Gets the state of a projection as an untyped document.
	/// </summary>
	/// <param name="name"></param>
	/// <param name="partition"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	public async ValueTask<Result<JsonDocument, GetStateError>> GetState(
		string name, string? partition = null, CancellationToken cancellationToken = default
	) {
		try {
			var value = await GetStateInternal(name, partition, cancellationToken).ConfigureAwait(false);

			await using var stream = new MemoryStream();
			await using var writer = new Utf8JsonWriter(stream);

			var serializer = new ValueSerializer();
			serializer.Write(writer, value, DefaultJsonSerializerOptions);
			stream.Position = 0;
			await writer.FlushAsync(cancellationToken).ConfigureAwait(false);

			return Result.Success<JsonDocument, GetStateError>(
				await JsonDocument
					.ParseAsync(stream, cancellationToken: cancellationToken)
					.ConfigureAwait(false)
			);
		} catch (Exception ex) when (ex.InnerException is RpcException rpcEx) {
			return Result.Failure<JsonDocument, GetStateError>(
				ex switch {
					AccessDeniedException     => rpcEx.AsAccessDeniedError(),
					NotAuthenticatedException => rpcEx.AsNotAuthenticatedError(),
					_                         => throw KurrentClientException.CreateUnknown(nameof(GetState), ex)
				}
			);
		} catch (Exception ex) {
			throw KurrentClientException.CreateUnknown(nameof(GetState), ex);
		}
	}

	/// <summary>
	/// Gets the state of a projection.
	/// </summary>
	/// <param name="name"></param>
	/// <param name="partition"></param>
	/// <param name="serializerOptions"></param>
	/// <param name="cancellationToken"></param>
	/// <typeparam name="T"></typeparam>
	/// <returns></returns>
	public async ValueTask<Result<T, GetStateError>> GetState<T>(
		string name, string? partition = null, JsonSerializerOptions? serializerOptions = null, CancellationToken cancellationToken = default
	) where T : notnull {
		try {
			var value = await GetStateInternal(name, partition, cancellationToken).ConfigureAwait(false);

			await using var stream = new MemoryStream();
			await using var writer = new Utf8JsonWriter(stream);

			var serializer = new ValueSerializer();
			serializer.Write(writer, value, DefaultJsonSerializerOptions);
			await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
			stream.Position = 0;

			return Result.Success<T, GetStateError>(JsonSerializer.Deserialize<T>(stream.ToArray(), serializerOptions)!);
		} catch (Exception ex) when (ex.InnerException is RpcException rpcEx) {
			return Result.Failure<T, GetStateError>(
				ex switch {
					AccessDeniedException     => rpcEx.AsAccessDeniedError(),
					NotAuthenticatedException => rpcEx.AsNotAuthenticatedError(),
					_                         => throw KurrentClientException.CreateUnknown(nameof(GetState), ex)
				}
			);
		} catch (Exception ex) {
			throw KurrentClientException.CreateUnknown(nameof(GetState), ex);
		}
	}

	async ValueTask<Value> GetStateInternal(
		string name, string? partition, CancellationToken cancellationToken
	) {
		using var call = ServiceClient.StateAsync(
			new StateReq {
				Options = new StateReq.Types.Options {
					Name      = name,
					Partition = partition ?? string.Empty
				}
			}
		  , cancellationToken: cancellationToken
		);

		var response = await call.ResponseAsync.ConfigureAwait(false);
		return response.State;
	}

	async ValueTask<Value> GetResultInternal(
		string name, string? partition, CancellationToken cancellationToken
	) {
		using var call = ServiceClient.ResultAsync(
			new ResultReq {
				Options = new ResultReq.Types.Options {
					Name      = name,
					Partition = partition ?? string.Empty
				}
			}
		  , cancellationToken: cancellationToken
		);

		var response = await call.ResponseAsync.ConfigureAwait(false);
		return response.Result;
	}

	class ValueSerializer : System.Text.Json.Serialization.JsonConverter<Value> {
		public override Value Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => throw new NotSupportedException();

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
