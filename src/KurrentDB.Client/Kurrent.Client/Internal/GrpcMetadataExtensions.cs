// using KurrentDB.Client;
//
// namespace Kurrent.Client;
//
// static class GrpcMetadataExtensions {
// 	public static bool TryGetValue(this global::Grpc.Core.Metadata metadata, string key, out string? value) {
// 		value = null;
//
// 		foreach (var entry in metadata) {
// 			if (entry.Key != key)
// 				continue;
//
// 			value = entry.Value;
// 			return true;
// 		}
//
// 		return false;
// 	}
// 	//
// 	// public static StreamState GetStreamState(this global::Grpc.Core.Metadata metadata, string key) =>
// 	// 	metadata.TryGetValue(key, out var s) && ulong.TryParse(s, out var value)
// 	// 		? value
// 	// 		: StreamState.NoStream;
// 	//
// 	// public static int GetIntValueOrDefault(this global::Grpc.Core.Metadata metadata, string key) =>
// 	// 	metadata.TryGetValue(key, out var s) && int.TryParse(s, out var value)
// 	// 		? value
// 	// 		: 0;
// }
