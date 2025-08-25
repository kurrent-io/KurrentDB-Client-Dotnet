using Grpc.Core;

namespace Kurrent.Client;

static class RpcMetadataExtensions {
    public static Metadata ExtractErrorData(this RpcException rex) {
        var meta = new Metadata()
            .With("RpcStatus", rex.Status)
            .With("RpcTrailers", new Metadata(rex.Trailers.ToDictionary(x => x.Key, static object? (kvp) => kvp.Value)))
            .WithMany(rex.Trailers.ToDictionary(x => $"RPC:{x.Key}", static object? (kvp) => kvp.Value));

        return meta;
    }

    public static Status GetRpcStatus(this Metadata errorData) =>
        errorData.GetRequired<Status>("RpcStatus");

    public static StatusCode GetRpcStatusCode(this Metadata errorData) =>
        errorData.GetRequired<Status>("RpcStatus").StatusCode;

    public static Metadata GetRpcTrailers(this Metadata errorData) =>
        errorData.GetRequired<Metadata>("RpcTrailers");

    public static bool TryGetRpcTrailerValue<T>(this Metadata errorData, string key, out T? value) =>
        errorData.GetRpcTrailers().TryGet(key, out value);

    public static T GetRpcTrailerValue<T>(this Metadata errorData, string key) =>
        errorData.GetRpcTrailers().GetRequired<T>(key);
}
