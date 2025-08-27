namespace KurrentDB.Diagnostics.Tracing;

static class TraceConstants {
	public const string TraceId = "$traceId";
	public const string SpanId  = "$spanId";

	public static class Tags {
		public const string DatabaseUser           = "db.user";
		public const string DatabaseSystemName     = "db.system.name";
		public const string DatabaseOperationName  = "db.operation.name";
		public const string DatabaseSubscriptionId = "db.kurrentdb.subscription.id";
		public const string DatabaseStream         = "db.kurrentdb.stream";
		public const string DatabaseRecordId       = "db.kurrentdb.record.id";
		public const string DatabaseSchemaName     = "db.kurrentdb.schema.name";
		public const string DatabaseSchemaFormat   = "db.kurrentdb.schema.format";
		public const string ServerAddress          = "server.address";
		public const string ServerPort             = "server.port";
	}

	public static class Operations {
		public const string Subscribe = "streams.subscribe";
		public const string Append    = "streams.multi-append";
	}
}
