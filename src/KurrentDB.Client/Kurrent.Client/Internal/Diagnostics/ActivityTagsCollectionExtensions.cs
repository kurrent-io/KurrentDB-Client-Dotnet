// ReSharper disable CheckNamespace

using System.Diagnostics;
using System.Runtime.CompilerServices;
using Kurrent.Client;
using KurrentDB.Client;
using KurrentDB.Diagnostics.Telemetry;

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
	public static ActivityTagsCollection WithGrpcChannelServerTags(this ActivityTagsCollection tags, ChannelInfo? channelInfo) {
		if (channelInfo is null)
			return tags;

		var authorityParts = channelInfo.Channel.Target.Split(':');

		return tags
			.WithRequiredTag(TelemetryTags.Server.Address, authorityParts[0])
			.WithRequiredTag(TelemetryTags.Server.Port, int.Parse(authorityParts[1]));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ActivityTagsCollection WithClientSettingsServerTags(this ActivityTagsCollection source, KurrentClientOptions settings) {
		if (settings.Endpoints.Length != 1)
			return source;

		var gossipSeed = settings.Endpoints[0];

		return source
			.WithRequiredTag(TelemetryTags.Server.Address, gossipSeed.Host)
			.WithRequiredTag(TelemetryTags.Server.Port, gossipSeed.Port);
	}
}
