// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract

using System.Diagnostics;
using KurrentDB.Diagnostics;
using KurrentDB.Diagnostics.Telemetry;
using KurrentDB.Diagnostics.Tracing;

namespace KurrentDB.Client.Diagnostics;

static class ActivitySourceExtensions {
	public static async ValueTask<T> TraceClientOperation<T>(
		this ActivitySource source,
		Func<ValueTask<T>> tracedOperation,
		string operationName,
		ActivityTagsCollection? tags = null
	) {
		using var activity = StartActivity(source, operationName, ActivityKind.Client, tags, Activity.Current?.Context);

		try {
			var res = await tracedOperation().ConfigureAwait(false);
			activity?.StatusOk();
			return res;
		} catch (Exception ex) {
			activity?.StatusError(ex);
			throw;
		}
	}

	public static void TraceSubscriptionEvent(
		this ActivitySource source,
		string? subscriptionId,
		ResolvedEvent resolvedEvent,
		ChannelInfo channelInfo,
		KurrentDBClientSettings settings,
		UserCredentials? userCredentials
	) {
		if (source.HasNoActiveListeners() || resolvedEvent.Event is null)
			return;

		var parentContext = resolvedEvent.Event.Metadata.ExtractPropagationContext();

		if (parentContext == default(ActivityContext)) return;

		var tags = new ActivityTagsCollection()
			.WithRequiredTag(TelemetryTags.KurrentDB.Stream, resolvedEvent.OriginalEvent.EventStreamId)
			.WithOptionalTag(TelemetryTags.KurrentDB.SubscriptionId, subscriptionId)
			.WithRequiredTag(TelemetryTags.KurrentDB.EventId, resolvedEvent.OriginalEvent.EventId.ToString())
			.WithRequiredTag(TelemetryTags.KurrentDB.EventType, resolvedEvent.OriginalEvent.EventType)
			// Ensure consistent server.address attribute when connecting to cluster via dns discovery
			.WithGrpcChannelServerTags(channelInfo)
			.WithClientSettingsServerTags(settings)
			.WithOptionalTag(
				TelemetryTags.Database.User,
				userCredentials?.Username ?? settings.DefaultCredentials?.Username
			);

		StartActivity(source, TracingConstants.Operations.Subscribe, ActivityKind.Consumer, tags, parentContext)
			?.Dispose();
	}

	static Activity? StartActivity(
		this ActivitySource source,
		string operationName, ActivityKind activityKind, ActivityTagsCollection? tags = null,
		ActivityContext? parentContext = null
	) {
		if (source.HasNoActiveListeners())
			return null;

		(tags ??= new ActivityTagsCollection())
			.WithRequiredTag(TelemetryTags.Database.System, "kurrent")
			.WithRequiredTag(TelemetryTags.Database.Operation, operationName);

		return source
			.CreateActivity(
				operationName,
				activityKind,
				parentContext ?? default,
				tags,
				idFormat: ActivityIdFormat.W3C
			)
			?.Start();
	}

	static bool HasNoActiveListeners(this ActivitySource source) => !source.HasListeners();
}
