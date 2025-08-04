namespace KurrentDB.Client;

static class Constants {
	public static class LegacyExceptions {
		public const string ExceptionKey = "exception";

		public const string AccessDenied                    = "access-denied";
		public const string InvalidTransaction              = "invalid-transaction";
		public const string StreamDeleted                   = "stream-deleted";
		public const string WrongExpectedVersion            = "wrong-expected-version";
		public const string StreamNotFound                  = "stream-not-found";
		public const string MaximumAppendSizeExceeded       = "maximum-append-size-exceeded";
		public const string MissingRequiredMetadataProperty = "missing-required-metadata-property";
		public const string NotLeader                       = "not-leader";

		public const string PersistentSubscriptionFailed       = "persistent-subscription-failed";
		public const string PersistentSubscriptionDoesNotExist = "persistent-subscription-does-not-exist";
		public const string PersistentSubscriptionExists       = "persistent-subscription-exists";
		public const string MaximumSubscribersReached          = "maximum-subscribers-reached";
		public const string PersistentSubscriptionDropped      = "persistent-subscription-dropped";

		public const string UserNotFound = "user-not-found";
		public const string UserConflict = "user-conflict";

		public const string ScavengeNotFound = "scavenge-not-found";

		public const string ExpectedVersion            = "expected-version";
		public const string ActualVersion              = "actual-version";
		public const string StreamName                 = "stream-name";
		public const string GroupName                  = "group-name";
		public const string Reason                     = "reason";
		public const string MaximumAppendSize          = "maximum-append-size";
		public const string RequiredMetadataProperties = "required-metadata-properties";
		public const string ScavengeId                 = "scavenge-id";
		public const string LeaderEndpointHost         = "leader-endpoint-host";
		public const string LeaderEndpointPort         = "leader-endpoint-port";

		public const string LoginName = "login-name";
	}

	public static class Metadata {
		public const string Type        = "type";
        public const string Created     = "created";
        public const string ContentType = "content-type";

        public static readonly string[] RequiredMetadata = [Type, ContentType];

		public static class ContentTypes {
			public const string ApplicationJson        = "application/json";
			public const string ApplicationOctetStream = "application/octet-stream";
		}
	}

	public static class Headers {
		public const string Authorization = "authorization";
		public const string BasicScheme   = "Basic";
		public const string BearerScheme  = "Bearer";

		public const string ConnectionName = "connection-name";
		public const string RequiresLeader = "requires-leader";

		#region Client Metrics

		/// <summary>
		/// Name of the client (e.g., dotnet, java, go, python, rust, nodejs)
		/// </summary>
		public const string ClientName = "kurrentdb.client.name";

		/// <summary>
		/// Version of the client (e.g., 2.0.1)
		/// </summary>
		public const string ClientVersion = "kurrentdb.client.version";

		/// <summary>
		/// Operating system name (e.g., linux, windows, darwin)
		/// </summary>
		public const string OsName = "kurrentdb.client.env.os.name";

		/// <summary>
		/// Operating system version (e.g., 10.0.22621)
		/// </summary>
		public const string OsVersion = "kurrentdb.client.env.os.version";

		/// <summary>
		/// Runtime or interpreter name (e.g., .NET, Node.js, Python)
		/// </summary>
		public const string RuntimeName = "kurrentdb.client.env.runtime.name";

		/// <summary>
		/// Runtime or interpreter version (e.g., 8.0.3, 20.10.0, 3.11.1)
		/// </summary>
		public const string RuntimeVersion = "kurrentdb.client.env.runtime.version";

		/// <summary>
		/// CPU architecture (e.g., x64, arm64)
		/// </summary>
		public const string HostArchitecture = "kurrentdb.client.env.architecture";

		#endregion
	}
}
