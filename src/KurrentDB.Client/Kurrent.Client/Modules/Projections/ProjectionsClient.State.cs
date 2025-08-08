// ReSharper disable SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault

using System.Text.Json;
using Google.Protobuf;
using Grpc.Core;
using KurrentDB.Protocol.Projections.V1;

namespace Kurrent.Client.Projections;

public partial class ProjectionsClient {

}

//
// public async ValueTask<Result<JsonDocument, GetProjectionResultError>> GetResult(
//     ProjectionName name,
//     string? partition = null,
//     JsonSerializerOptions? serializerOptions = null,
//     CancellationToken cancellationToken = default
// ) {
//     name.ThrowIfInvalid();
//
//     try {
//         var request = new ResultReq {
//             Options = new() {
//                 Name      = name,
//                 Partition = partition ?? string.Empty
//             }
//         };
//
//         var response = await ServiceClient
//             .ResultAsync(request, cancellationToken: cancellationToken)
//             .ConfigureAwait(false);
//
//         return JsonDocument.Parse(JsonFormatter.Default.Format(response.Result));
//     }
//     catch (RpcException rex) {
//         return Result.Failure<JsonDocument, GetProjectionResultError>(rex.StatusCode switch {
//             StatusCode.PermissionDenied => new ErrorDetails.AccessDenied(),
//             StatusCode.NotFound         => new ErrorDetails.NotFound(),
//             _                           => throw rex.WithOriginalCallStack()
//         });
//     }
// }
//
// public partial class ProjectionsClient {
// 	static readonly JsonSerializerOptions DefaultJsonSerializerOptions = new();
//
// 	public async ValueTask<Result<T, GetProjectionResultError>> GetResult<T>(
// 		ProjectionName name,
// 		string? partition = null,
// 		JsonSerializerOptions? serializerOptions = null,
// 		CancellationToken cancellationToken = default
// 	) where T : notnull {
//         name.ThrowIfInvalid();
//
// 		try {
//             var request = new ResultReq {
//                 Options = new() {
//                     Name      = name,
//                     Partition = partition ?? string.Empty
//                 }
//             };
//
//             var response = await ServiceClient
//                 .ResultAsync(request, cancellationToken: cancellationToken)
//                 .ConfigureAwait(false);
//
//             var result = JsonDocument.Parse(JsonFormatter.Default.Format(response.Result));
//
//             using var stream = await ConvertValueToStream(response.Result, cancellationToken).ConfigureAwait(false);
//
//             var result = await JsonSerializer
//                 .DeserializeAsync<T>(stream, serializerOptions, cancellationToken)
//                 .ConfigureAwait(false)!;
//
//             return result;
//         }
//         catch (RpcException rex) {
//             return Result.Failure<T, GetProjectionResultError>(rex.StatusCode switch {
//                 StatusCode.PermissionDenied => new ErrorDetails.AccessDenied(),
//                 StatusCode.NotFound         => new ErrorDetails.NotFound(),
//                 _                           => throw rex.WithOriginalCallStack()
//             });
//         }
// 	}
//
//     async ValueTask<MemoryStream> GetResultStream(string name, string? partition, CancellationToken cancellationToken) {
//         var request = new ResultReq {
//             Options = new() {
//                 Name      = name,
//                 Partition = partition ?? string.Empty
//             }
//         };
//
//         var response = await ServiceClient
//             .ResultAsync(request, cancellationToken: cancellationToken)
//             .ConfigureAwait(false);
//
//         return await ConvertValueToStream(response.Result, cancellationToken);
//     }
//
// 	public async ValueTask<Result<JsonDocument, GetProjectionStateError>> GetState(
// 		string name, string? partition = null, CancellationToken cancellationToken = default
// 	) {
// 		try {
// 			using var stream = await GetStateStream(name, partition, cancellationToken).ConfigureAwait(false);
//
//             var result = await JsonDocument
//                 .ParseAsync(stream, cancellationToken: cancellationToken)
//                 .ConfigureAwait(false)!;
//
//             return result;
//         }
//         catch (RpcException rex) {
//             return Result.Failure<JsonDocument, GetProjectionStateError>(rex.StatusCode switch {
//                 StatusCode.PermissionDenied => new ErrorDetails.AccessDenied(),
//                 StatusCode.NotFound         => new ErrorDetails.NotFound(),
//                 _                           => throw rex.WithOriginalCallStack()
//             });
//         }
// 	}
//
// 	public async ValueTask<Result<T, GetProjectionStateError>> GetState<T>(
// 		string name, string? partition = null, JsonSerializerOptions? serializerOptions = null, CancellationToken cancellationToken = default
// 	) where T : notnull {
// 		try {
//             using var stream = await GetStateStream(name, partition, cancellationToken).ConfigureAwait(false);
//
//             var result = await JsonSerializer
//                 .DeserializeAsync<T>(stream, serializerOptions, cancellationToken)
//                 .ConfigureAwait(false)!;
//
//             return result;
//         }
//         catch (RpcException rex) {
//             return Result.Failure<T, GetProjectionStateError>(rex.StatusCode switch {
//                 StatusCode.PermissionDenied => new ErrorDetails.AccessDenied(),
//                 StatusCode.NotFound         => new ErrorDetails.NotFound(),
//                 _                           => throw rex.WithOriginalCallStack()
//             });
//         }
// 	}
//
//     async ValueTask<MemoryStream> GetStateStream(string name, string? partition, CancellationToken cancellationToken) {
//         var request = new StateReq {
//             Options = new() {
//                 Name      = name,
//                 Partition = partition ?? string.Empty
//             }
//         };
//
//         var response = await ServiceClient
//             .StateAsync(request, cancellationToken: cancellationToken)
//             .ConfigureAwait(false);
//
//         return await ConvertValueToStream(response.State, cancellationToken);
//     }
//
//     static async ValueTask<MemoryStream> ConvertValueToStream(Value value, CancellationToken cancellationToken) {
//         var serializer = new ValueSerializer();
//         var stream     = new MemoryStream();
//
//         await using var writer = new Utf8JsonWriter(stream);
//
//         serializer.Write(writer, value, DefaultJsonSerializerOptions);
//         await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
//         stream.Position = 0;
//
//         return stream;
//     }
//
// 	class ValueSerializer : JsonConverter<Value> {
// 		public override Value Read(ref Utf8JsonReader reader, System.Type typeToConvert, JsonSerializerOptions options) =>
//             throw new NotSupportedException();
//
// 		public override void Write(Utf8JsonWriter writer, Value value, JsonSerializerOptions options) {
// 			switch (value.KindCase) {
// 				case Value.KindOneofCase.None:
// 					break;
//
// 				case Value.KindOneofCase.BoolValue:
// 					writer.WriteBooleanValue(value.BoolValue);
// 					break;
//
// 				case Value.KindOneofCase.NullValue:
// 					writer.WriteNullValue();
// 					break;
//
// 				case Value.KindOneofCase.NumberValue:
// 					writer.WriteNumberValue(value.NumberValue);
// 					break;
//
// 				case Value.KindOneofCase.StringValue:
// 					writer.WriteStringValue(value.StringValue);
// 					break;
//
// 				case Value.KindOneofCase.ListValue:
// 					writer.WriteStartArray();
// 					foreach (var item in value.ListValue.Values)
//                         Write(writer, item, options);
//
//                     writer.WriteEndArray();
// 					break;
//
// 				case Value.KindOneofCase.StructValue:
// 					writer.WriteStartObject();
// 					foreach (var map in value.StructValue.Fields) {
// 						writer.WritePropertyName(map.Key);
// 						Write(writer, map.Value, options);
// 					}
//
// 					writer.WriteEndObject();
// 					break;
// 			}
// 		}
// 	}
// }

// public partial class ProjectionsClient {
// 	static readonly JsonSerializerOptions DefaultJsonSerializerOptions = new();
//
// 	public async ValueTask<Result<JsonDocument, GetProjectionResultError>> GetResult(
// 		string name, string? partition = null, CancellationToken cancellationToken = default
// 	) {
// 		try {
// 			var stream = await GetResultStream(name, partition, cancellationToken).ConfigureAwait(false);
//
//             var result = await JsonDocument
//                 .ParseAsync(stream, cancellationToken: cancellationToken)
//                 .ConfigureAwait(false);
//
//             return result;
//         }
//         catch (RpcException rex) {
//             return Result.Failure<JsonDocument, GetProjectionResultError>(rex.StatusCode switch {
//                 StatusCode.PermissionDenied => new ErrorDetails.AccessDenied(),
//                 StatusCode.NotFound         => new ErrorDetails.NotFound(),
//                 _                           => throw rex.WithOriginalCallStack()
//             });
//         }
// 	}
//
// 	public async ValueTask<Result<T, GetProjectionResultError>> GetResult<T>(
// 		string name,
// 		string? partition = null,
// 		JsonSerializerOptions? serializerOptions = null,
// 		CancellationToken cancellationToken = default
// 	) where T : notnull {
// 		try {
// 			using var stream = await GetResultStream(name, partition, cancellationToken).ConfigureAwait(false);
//
//             var result = await JsonSerializer
//                 .DeserializeAsync<T>(stream, serializerOptions, cancellationToken)
//                 .ConfigureAwait(false)!;
//
//             return result;
//         }
//         catch (RpcException rex) {
//             return Result.Failure<T, GetProjectionResultError>(rex.StatusCode switch {
//                 StatusCode.PermissionDenied => new ErrorDetails.AccessDenied(),
//                 StatusCode.NotFound         => new ErrorDetails.NotFound(),
//                 _                           => throw rex.WithOriginalCallStack()
//             });
//         }
// 	}
//
//     async ValueTask<MemoryStream> GetResultStream(string name, string? partition, CancellationToken cancellationToken) {
//         var request = new ResultReq {
//             Options = new() {
//                 Name      = name,
//                 Partition = partition ?? string.Empty
//             }
//         };
//
//         var response = await ServiceClient
//             .ResultAsync(request, cancellationToken: cancellationToken)
//             .ConfigureAwait(false);
//
//         return await ConvertValueToStream(response.Result, cancellationToken);
//     }
//
// 	public async ValueTask<Result<JsonDocument, GetProjectionStateError>> GetState(
// 		string name, string? partition = null, CancellationToken cancellationToken = default
// 	) {
// 		try {
// 			using var stream = await GetStateStream(name, partition, cancellationToken).ConfigureAwait(false);
//
//             var result = await JsonDocument
//                 .ParseAsync(stream, cancellationToken: cancellationToken)
//                 .ConfigureAwait(false)!;
//
//             return result;
//         }
//         catch (RpcException rex) {
//             return Result.Failure<JsonDocument, GetProjectionStateError>(rex.StatusCode switch {
//                 StatusCode.PermissionDenied => new ErrorDetails.AccessDenied(),
//                 StatusCode.NotFound         => new ErrorDetails.NotFound(),
//                 _                           => throw rex.WithOriginalCallStack()
//             });
//         }
// 	}
//
// 	public async ValueTask<Result<T, GetProjectionStateError>> GetState<T>(
// 		string name, string? partition = null, JsonSerializerOptions? serializerOptions = null, CancellationToken cancellationToken = default
// 	) where T : notnull {
// 		try {
//             using var stream = await GetStateStream(name, partition, cancellationToken).ConfigureAwait(false);
//
//             var result = await JsonSerializer
//                 .DeserializeAsync<T>(stream, serializerOptions, cancellationToken)
//                 .ConfigureAwait(false)!;
//
//             return result;
//         }
//         catch (RpcException rex) {
//             return Result.Failure<T, GetProjectionStateError>(rex.StatusCode switch {
//                 StatusCode.PermissionDenied => new ErrorDetails.AccessDenied(),
//                 StatusCode.NotFound         => new ErrorDetails.NotFound(),
//                 _                           => throw rex.WithOriginalCallStack()
//             });
//         }
// 	}
//
//     async ValueTask<MemoryStream> GetStateStream(string name, string? partition, CancellationToken cancellationToken) {
//         var request = new StateReq {
//             Options = new() {
//                 Name      = name,
//                 Partition = partition ?? string.Empty
//             }
//         };
//
//         var response = await ServiceClient
//             .StateAsync(request, cancellationToken: cancellationToken)
//             .ConfigureAwait(false);
//
//         return await ConvertValueToStream(response.State, cancellationToken);
//     }
//
//     static async ValueTask<MemoryStream> ConvertValueToStream(Value value, CancellationToken cancellationToken) {
//         var serializer = new ValueSerializer();
//         var stream     = new MemoryStream();
//
//         await using var writer = new Utf8JsonWriter(stream);
//
//         serializer.Write(writer, value, DefaultJsonSerializerOptions);
//         await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
//         stream.Position = 0;
//
//         return stream;
//     }
//
// 	class ValueSerializer : JsonConverter<Value> {
// 		public override Value Read(ref Utf8JsonReader reader, System.Type typeToConvert, JsonSerializerOptions options) =>
//             throw new NotSupportedException();
//
// 		public override void Write(Utf8JsonWriter writer, Value value, JsonSerializerOptions options) {
// 			switch (value.KindCase) {
// 				case Value.KindOneofCase.None:
// 					break;
//
// 				case Value.KindOneofCase.BoolValue:
// 					writer.WriteBooleanValue(value.BoolValue);
// 					break;
//
// 				case Value.KindOneofCase.NullValue:
// 					writer.WriteNullValue();
// 					break;
//
// 				case Value.KindOneofCase.NumberValue:
// 					writer.WriteNumberValue(value.NumberValue);
// 					break;
//
// 				case Value.KindOneofCase.StringValue:
// 					writer.WriteStringValue(value.StringValue);
// 					break;
//
// 				case Value.KindOneofCase.ListValue:
// 					writer.WriteStartArray();
// 					foreach (var item in value.ListValue.Values)
//                         Write(writer, item, options);
//
//                     writer.WriteEndArray();
// 					break;
//
// 				case Value.KindOneofCase.StructValue:
// 					writer.WriteStartObject();
// 					foreach (var map in value.StructValue.Fields) {
// 						writer.WritePropertyName(map.Key);
// 						Write(writer, map.Value, options);
// 					}
//
// 					writer.WriteEndObject();
// 					break;
// 			}
// 		}
// 	}
// }
