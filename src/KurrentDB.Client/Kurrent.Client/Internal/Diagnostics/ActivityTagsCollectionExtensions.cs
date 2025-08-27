// ReSharper disable CheckNamespace

using System.Diagnostics;
using System.Net;
using System.Runtime.CompilerServices;
using KurrentDB.Diagnostics.Tracing;

namespace KurrentDB.Diagnostics;

static class ActivityTagsCollectionExtensions {
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ActivityTagsCollection WithRequiredTag(this ActivityTagsCollection source, string key, object? value) {
		source[key] = value ?? throw new ArgumentNullException(key);
		return source;
	}

	/// <summary>
	/// - If the key previously existed in the collection and the value is <see langword="null" />, the collection item matching the key will get removed from the collection.
	/// - If the key previously existed in the collection and the value is not <see langword="null" />, the value will replace the old value stored in the collection.
	/// - Otherwise, a new item will get added to the collection.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ActivityTagsCollection WithOptionalTag(this ActivityTagsCollection source, string key, object? value) {
		source[key] = value;
		return source;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ActivityTagsCollection WithGrpcChannelServerTags(this ActivityTagsCollection tags, string target) {
        var authorityParts = target.Split(':');

        return tags
            .WithRequiredTag(TraceConstants.Tags.ServerAddress, authorityParts[0])
            .WithRequiredTag(TraceConstants.Tags.ServerPort, int.Parse(authorityParts[1]));
    }

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ActivityTagsCollection WithClientSettingsServerTags(this ActivityTagsCollection source, DnsEndPoint[] endpoints) {
        if (endpoints.Length != 1)
            return source;

        var gossipSeed = endpoints[0];

        return source
            .WithRequiredTag(TraceConstants.Tags.ServerAddress, gossipSeed.Host)
            .WithRequiredTag(TraceConstants.Tags.ServerPort, gossipSeed.Port);
    }
}
