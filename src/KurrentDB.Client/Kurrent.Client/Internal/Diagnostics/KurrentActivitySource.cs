// ReSharper disable ConvertIfStatementToReturnStatement

using System.Diagnostics;
using Kurrent.Client;
using Kurrent.Client.Streams;
using KurrentDB.Diagnostics.Tracing;

namespace KurrentDB.Diagnostics;

public static class KurrentActivitySource {
	static KurrentActivitySource() {
		var clientName    = AppVersionInfo.Current.ProductName ?? "KurrentDB .NET Client";
		var clientVersion = AppVersionInfo.Current.ProductVersion ?? AppVersionInfo.Current.FileVersion ?? "0.0.0";

		ActivitySource = new(clientName, clientVersion);
	}

	public static ActivitySource ActivitySource { get; }

	public static Activity? StartSubscriptionActivity(ReadMessage message, ActivityTagsCollection? tags = null) {
		if (!ActivitySource.HasListeners())
			return null;

		if (!message.IsRecord)
			return null;

		tags ??= new();

		var metadata = message.AsRecord.Metadata;

		metadata.TryGet<ActivityTraceId>(TraceConstants.TraceId, out var traceId);
		metadata.TryGet<ActivitySpanId>(TraceConstants.SpanId, out var spanId);

		var parentContext = new ActivityContext(
			traceId, spanId, ActivityTraceFlags.Recorded,
			isRemote: true
		);

		return StartActivity(TraceConstants.Operations.Subscribe, ActivityKind.Consumer, tags, parentContext);
	}

	public static Activity? StartAppendActivity(ActivityTagsCollection tags) {
		if (!ActivitySource.HasListeners())
			return null;

		return StartActivity(TraceConstants.Operations.Append, ActivityKind.Producer, tags, Activity.Current?.Context);
	}

	static Activity? StartActivity(string operationName, ActivityKind kind, ActivityTagsCollection tags, ActivityContext? parentContext) {
		var activity = ActivitySource.StartActivity(
			kind, name: operationName, tags: tags,
			parentContext: parentContext ?? default
		);

		return activity;
	}
}
