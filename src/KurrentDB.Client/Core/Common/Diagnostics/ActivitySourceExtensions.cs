// ReSharper disable ConvertIfStatementToSwitchStatement
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract

using System.Diagnostics;
using KurrentDB.Diagnostics;
using KurrentDB.Diagnostics.Telemetry;
using KurrentDB.Protocol.Streams.V2;
using static KurrentDB.Diagnostics.Tracing.TracingConstants;

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

		StartActivity(source, Operations.Subscribe, ActivityKind.Consumer, tags, parentContext)
			?.Dispose();
	}

	public static async IAsyncEnumerable<(Activity? Activity, AppendStreamRequest Request)> InstrumentAppendOperations(
		this ActivitySource source,
		IAsyncEnumerable<AppendStreamRequest> requests,
		Func<AppendStreamRequest, ActivityTagsCollection> createTags
	) {
		var currentActivity = Activity.Current;
		var startTime = DateTime.UtcNow;

		try {
			Activity.Current = null;

			await foreach (var request in requests) {
				Activity? activity = null;

				if (source.HasListeners()) {
					activity = StartActivity(source, Operations.Append, ActivityKind.Client, createTags(request), currentActivity?.Context);
					activity?.SetStartTime(startTime);
				}

				yield return (activity, request);
			}
		}
		finally {
			Activity.Current = currentActivity;
		}
	}

	public static async ValueTask CompleteAppendInstrumentation(
		this ActivitySource source,
		IAsyncEnumerable<(Activity? Activity, AppendStreamRequest Request)> observables,
		MultiStreamAppendResponse response
	) {
		if (source.HasNoActiveListeners())
			return;

		var endTime    = DateTime.UtcNow;
		var resultCase = response.ResultCase;
		var failures   = response.ResultCase is MultiStreamAppendResponse.ResultOneofCase.Failure ? response.Failure.Map() : [];

		var activities = await observables
			.Where(tr => tr.Activity is not null)
			.Select(tr => tr.Activity!)
			.ToListAsync();

		foreach (var activity in activities) {
			activity.SetEndTime(endTime);

			if (resultCase is MultiStreamAppendResponse.ResultOneofCase.Success)
				activity.StatusOk();

			else if (resultCase is MultiStreamAppendResponse.ResultOneofCase.Failure) {
				activity.SetStatus(ActivityStatusCode.Error);
				failures.ForEach(error => activity.AddException(error));
			}

			activity.Dispose();
		}
	}

	static Activity? StartActivity(
		this ActivitySource source,
		string operationName, ActivityKind activityKind, ActivityTagsCollection? tags = null,
		ActivityContext? parentContext = null
	) {
		if (source.HasNoActiveListeners())
			return null;

		(tags ??= new ActivityTagsCollection())
			.WithRequiredTag(TelemetryTags.Database.System, KurrentDBClientDiagnostics.InstrumentationName)
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
