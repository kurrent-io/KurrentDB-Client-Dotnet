using Grpc.Core;
using static KurrentDB.Protocol.Users.V1.ServerFeatures;

namespace KurrentDB.Client {
	internal class GrpcServerCapabilitiesClient : IServerCapabilitiesClient {
		private readonly KurrentDBClientSettings _settings;

		public GrpcServerCapabilitiesClient(KurrentDBClientSettings settings) {
			_settings = settings;
		}

		public async Task<ServerCapabilities> GetAsync(
			CallInvoker callInvoker,
			CancellationToken cancellationToken) {

			var client = new ServerFeaturesClient(callInvoker);
			using var call = client.GetSupportedMethodsAsync(
				new(),
				KurrentDBCallOptions.CreateNonStreaming(
					_settings,
					_settings.ConnectivitySettings.GossipTimeout,
					null,
					cancellationToken));

			try {
				var supportsBatchAppend = false;
				var supportsPersistentSubscriptionsToAll = false;
				var supportsPersistentSubscriptionsGetInfo = false;
				var supportsPersistentSubscriptionsRestartSubsystem = false;
				var supportsPersistentSubscriptionsReplayParked = false;
				var supportsPersistentSubscriptionsList = false;
				var supportsMultiStreamAppend = false;

				var response = await call.ResponseAsync.ConfigureAwait(false);

				foreach (var supportedMethod in response.Methods) {
					switch (supportedMethod.ServiceName, supportedMethod.MethodName) {
						case ("event_store.client.streams.streams", "batchappend"):
							supportsBatchAppend = true;
							continue;
						case ("event_store.client.persistent_subscriptions.persistentsubscriptions", "read"):
							supportsPersistentSubscriptionsToAll = supportedMethod.Features.Contains("all");
							continue;
						case ("event_store.client.persistent_subscriptions.persistentsubscriptions", "getinfo"):
							supportsPersistentSubscriptionsGetInfo = true;
							continue;
						case ("event_store.client.persistent_subscriptions.persistentsubscriptions", "restartsubsystem"):
							supportsPersistentSubscriptionsRestartSubsystem = true;
							continue;
						case ("event_store.client.persistent_subscriptions.persistentsubscriptions", "replayparked"):
							supportsPersistentSubscriptionsReplayParked = true;
							continue;
						case ("event_store.client.persistent_subscriptions.persistentsubscriptions", "list"):
							supportsPersistentSubscriptionsList = true;
							continue;
						case ("kurrentdb.protocol.v2.streamsservice", "multistreamappend"):
							supportsMultiStreamAppend = true;
							continue;
					}
				}

				return new(
					SupportsBatchAppend: supportsBatchAppend,
					SupportsPersistentSubscriptionsToAll: supportsPersistentSubscriptionsToAll,
					SupportsPersistentSubscriptionsGetInfo: supportsPersistentSubscriptionsGetInfo,
					SupportsPersistentSubscriptionsRestartSubsystem: supportsPersistentSubscriptionsRestartSubsystem,
					SupportsPersistentSubscriptionsReplayParked: supportsPersistentSubscriptionsReplayParked,
					SupportsPersistentSubscriptionsList: supportsPersistentSubscriptionsList,
					SupportsMultiStreamAppend: supportsMultiStreamAppend);

			} catch (Exception ex) when (ex.GetBaseException() is RpcException rpcException &&
				rpcException.StatusCode == StatusCode.Unimplemented) {

				return new();
			}
		}
	}
}
