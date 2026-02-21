# KurrentDB .NET Client SDK — API Reference

Comprehensive reference for the `KurrentDB.Client` NuGet package.

## Connection String Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `tls` | bool | `true` | Enable TLS encryption |
| `tlsVerifyCert` | bool | `true` | Verify server TLS certificate |
| `tlsCaFile` | string | — | Path to CA certificate file for custom CAs |
| `connectionName` | string | — | Identifies the client in server logs |
| `defaultDeadline` | int | `10000` | Default operation timeout in milliseconds |
| `keepAliveInterval` | int | `10000` | gRPC keep-alive ping interval (ms), `-1` for infinite |
| `keepAliveTimeout` | int | `10000` | gRPC keep-alive timeout (ms), `-1` for infinite |
| `nodePreference` | string | `leader` | Preferred node: `leader`, `follower`, `random`, `readonlyreplica` |
| `maxDiscoverAttempts` | int | `10` | Maximum gossip discovery attempts |
| `discoveryInterval` | int | `100` | Gossip discovery polling interval (ms) |
| `gossipTimeout` | int | `5000` | Gossip request timeout (ms) |
| `throwOnAppendFailure` | bool | `true` | Throw on append failure vs return `WrongExpectedVersionResult` |
| `userCertFile` | string | — | Path to user certificate PEM file (certificate auth) |
| `userKeyFile` | string | — | Path to user private key PEM file (certificate auth) |

**Connection string format:**
```
scheme://[user:password@]host[:port][,host[:port]...][?key=value[&key=value...]]
```

**Supported schemes:** `esdb`, `esdb+discover`, `kdb`, `kdb+discover`, `kurrent`, `kurrent+discover`, `kurrentdb`, `kurrentdb+discover`

The `+discover` suffix enables cluster gossip discovery. Without it, the client connects directly to the specified node(s).

## KurrentDBClientSettings

```csharp
public class KurrentDBClientSettings {
    public IEnumerable<Interceptor>? Interceptors { get; set; }
    public string? ConnectionName { get; set; }
    public Func<HttpMessageHandler>? CreateHttpMessageHandler { get; set; }
    public ILoggerFactory? LoggerFactory { get; set; }
    public ChannelCredentials? ChannelCredentials { get; set; }
    public KurrentDBClientOperationOptions OperationOptions { get; set; }
    public KurrentDBClientConnectivitySettings ConnectivitySettings { get; set; }
    public UserCredentials? DefaultCredentials { get; set; }
    public TimeSpan? DefaultDeadline { get; set; }

    public static KurrentDBClientSettings Create(string connectionString);
}
```

### KurrentDBClientConnectivitySettings

```csharp
public class KurrentDBClientConnectivitySettings {
    public Uri? Address { get; set; }
    public int MaxDiscoverAttempts { get; set; }          // default: 10
    public DnsEndPoint[]? DnsGossipSeeds { get; set; }
    public IPEndPoint[]? IpGossipSeeds { get; set; }
    public EndPoint[] GossipSeeds { get; }
    public TimeSpan GossipTimeout { get; set; }           // default: 5s
    public TimeSpan DiscoveryInterval { get; set; }       // default: 100ms
    public NodePreference NodePreference { get; set; }    // default: Leader
    public TimeSpan KeepAliveInterval { get; set; }       // default: 10s
    public TimeSpan KeepAliveTimeout { get; set; }        // default: 10s
    public bool IsSingleNode { get; }
    public bool Insecure { get; set; }
    public bool TlsVerifyCert { get; set; }               // default: true
    public X509Certificate2? TlsCaFile { get; set; }
    public X509Certificate2? ClientCertificate { get; set; }

    public static KurrentDBClientConnectivitySettings Default { get; }
}
```

### KurrentDBClientOperationOptions

```csharp
public class KurrentDBClientOperationOptions {
    public bool ThrowOnAppendFailure { get; set; }        // default: true
    public int BatchAppendSize { get; set; }
    public Func<UserCredentials, CancellationToken, ValueTask<string>> GetAuthenticationHeaderValue { get; set; }

    public KurrentDBClientOperationOptions Clone();
    public static KurrentDBClientOperationOptions Default { get; }
}
```

## EventData

```csharp
public readonly struct EventData {
    public EventData(
        Uuid eventId,
        string type,
        ReadOnlyMemory<byte> data,
        ReadOnlyMemory<byte>? metadata = null,
        string contentType = "application/json"
    );

    public readonly Uuid EventId;
    public readonly string Type;
    public readonly ReadOnlyMemory<byte> Data;
    public readonly ReadOnlyMemory<byte> Metadata;
    public readonly string ContentType;
}
```

## Uuid

```csharp
public readonly struct Uuid : IEquatable<Uuid> {
    public static readonly Uuid Empty;

    public static Uuid NewUuid();
    public static Uuid FromGuid(Guid value);
    public static Uuid Parse(string value);
    public static Uuid FromInt64(long msb, long lsb);

    public Guid ToGuid();
    public string ToString(string format);
}
```

## EventRecord

```csharp
public readonly struct EventRecord {
    public readonly string EventStreamId;
    public readonly Uuid EventId;
    public readonly StreamPosition EventNumber;
    public readonly string EventType;
    public readonly ReadOnlyMemory<byte> Data;
    public readonly ReadOnlyMemory<byte> Metadata;
    public readonly DateTime Created;
    public readonly Position Position;
    public readonly string ContentType;
}
```

## ResolvedEvent

```csharp
public readonly struct ResolvedEvent {
    public readonly EventRecord Event;
    public readonly EventRecord? Link;
    public readonly Position? OriginalPosition;

    public EventRecord OriginalEvent { get; }     // Link ?? Event
    public string OriginalStreamId { get; }
    public StreamPosition OriginalEventNumber { get; }
    public bool IsResolved { get; }                // Link != null
}
```

## StreamState

```csharp
public readonly struct StreamState : IEquatable<StreamState> {
    public static readonly StreamState NoStream;       // stream must not exist
    public static readonly StreamState Any;            // no concurrency check
    public static readonly StreamState StreamExists;   // stream must exist (any revision)

    public static StreamState StreamRevision(ulong value);  // specific revision
    public static implicit operator StreamState(ulong value);

    public long ToInt64();
    public bool HasPosition { get; }
}
```

## Position Types

### Position (for $all stream)

```csharp
public readonly struct Position : IEquatable<Position>, IComparable<Position> {
    public static readonly Position Start;   // (0, 0)
    public static readonly Position End;     // (max, max)

    public Position(ulong commitPosition, ulong preparePosition);

    public readonly ulong CommitPosition;
    public readonly ulong PreparePosition;

    public static bool TryParse(string value, out Position? position);
}
```

### StreamPosition (for individual streams)

```csharp
public readonly struct StreamPosition : IEquatable<StreamPosition>, IComparable<StreamPosition> {
    public static readonly StreamPosition Start;  // 0
    public static readonly StreamPosition End;    // max

    public StreamPosition(ulong value);
    public static StreamPosition FromInt64(long value);
    public static StreamPosition FromStreamRevision(ulong revision);

    public StreamPosition Next();
    public ulong ToUInt64();
    public long ToInt64();

    public static implicit operator ulong(StreamPosition sp);
    public static implicit operator StreamPosition(ulong value);
}
```

### FromStream (subscription starting point)

```csharp
public readonly struct FromStream : IEquatable<FromStream>, IComparable<FromStream> {
    public static readonly FromStream Start;  // from beginning
    public static readonly FromStream End;    // live only

    public static FromStream After(StreamPosition streamPosition);
}
```

### FromAll (subscription starting point for $all)

```csharp
public readonly struct FromAll : IEquatable<FromAll>, IComparable<FromAll> {
    public static readonly FromAll Start;  // from beginning
    public static readonly FromAll End;    // live only

    public static FromAll After(Position position);
}
```

## UserCredentials

```csharp
public class UserCredentials {
    public UserCredentials(string username, string password);
    public UserCredentials(string bearerToken);

    public string? Username { get; }
    public string? Password { get; }
}
```

## KurrentDBClient

### Append

```csharp
public Task<IWriteResult> AppendToStreamAsync(
    string streamName,
    StreamState expectedState,
    IEnumerable<EventData> eventData,
    Action<KurrentDBClientOperationOptions>? configureOperationOptions = null,
    TimeSpan? deadline = null,
    UserCredentials? userCredentials = null,
    CancellationToken cancellationToken = default);
```

### Multi-Stream Append (Server 25.1+)

```csharp
public ValueTask<MultiStreamAppendResponse> MultiStreamAppendAsync(
    IAsyncEnumerable<AppendStreamRequest> requests,
    CancellationToken cancellationToken = default);
```

**Supporting types:**

```csharp
public class AppendStreamRequest {
    public string Stream { get; set; }
    public StreamState ExpectedState { get; set; }
    public IAsyncEnumerable<EventData> Messages { get; set; }
}

public class AppendResponse {
    public string Stream { get; }
    public ulong StreamRevision { get; }
}

public class MultiStreamAppendResponse {
    public Position Position { get; }
    public IEnumerable<AppendResponse> Responses { get; }
}
```

### Read Stream

```csharp
public ReadStreamResult ReadStreamAsync(
    Direction direction,
    string streamName,
    StreamPosition revision,
    long maxCount = long.MaxValue,
    bool resolveLinkTos = false,
    TimeSpan? deadline = null,
    UserCredentials? userCredentials = null,
    CancellationToken cancellationToken = default);
```

**Return type:**

```csharp
public class ReadStreamResult : IAsyncEnumerable<ResolvedEvent> {
    public string StreamName { get; }
    public StreamPosition? FirstStreamPosition { get; }
    public StreamPosition? LastStreamPosition { get; }
    public IAsyncEnumerable<StreamMessage> Messages { get; }
    public Task<ReadState> ReadState { get; }
}

public enum ReadState { StreamNotFound, Ok }
```

### Read $all

```csharp
public ReadAllStreamResult ReadAllAsync(
    Direction direction,
    Position position,
    long maxCount = long.MaxValue,
    bool resolveLinkTos = false,
    TimeSpan? deadline = null,
    UserCredentials? userCredentials = null,
    CancellationToken cancellationToken = default);

// With server-side filtering
public ReadAllStreamResult ReadAllAsync(
    Direction direction,
    Position position,
    IEventFilter? eventFilter,
    long maxCount = long.MaxValue,
    bool resolveLinkTos = false,
    TimeSpan? deadline = null,
    UserCredentials? userCredentials = null,
    CancellationToken cancellationToken = default);
```

**Return type:**

```csharp
public class ReadAllStreamResult : IAsyncEnumerable<ResolvedEvent> {
    public Position? LastPosition { get; }
    public IAsyncEnumerable<StreamMessage> Messages { get; }
}
```

### Subscribe to Stream

```csharp
// New API (IAsyncEnumerable-based)
public StreamSubscriptionResult SubscribeToStream(
    string streamName,
    FromStream start,
    bool resolveLinkTos = false,
    UserCredentials? userCredentials = null,
    CancellationToken cancellationToken = default);

// Callback API
public Task<StreamSubscription> SubscribeToStreamAsync(
    string streamName,
    FromStream start,
    Func<StreamSubscription, ResolvedEvent, CancellationToken, Task> eventAppeared,
    bool resolveLinkTos = false,
    Action<StreamSubscription, SubscriptionDroppedReason, Exception?>? subscriptionDropped = default,
    UserCredentials? userCredentials = null,
    CancellationToken cancellationToken = default);
```

### Subscribe to $all

```csharp
// New API (IAsyncEnumerable-based)
public StreamSubscriptionResult SubscribeToAll(
    FromAll start,
    bool resolveLinkTos = false,
    SubscriptionFilterOptions? filterOptions = null,
    UserCredentials? userCredentials = null,
    CancellationToken cancellationToken = default);

// Callback API
public Task<StreamSubscription> SubscribeToAllAsync(
    FromAll start,
    Func<StreamSubscription, ResolvedEvent, CancellationToken, Task> eventAppeared,
    bool resolveLinkTos = false,
    Action<StreamSubscription, SubscriptionDroppedReason, Exception?>? subscriptionDropped = default,
    SubscriptionFilterOptions? filterOptions = null,
    UserCredentials? userCredentials = null,
    CancellationToken cancellationToken = default);
```

**Return type:**

```csharp
public class StreamSubscriptionResult : IAsyncEnumerable<ResolvedEvent>, IAsyncDisposable, IDisposable {
    public string? SubscriptionId { get; }
    public IAsyncEnumerable<StreamMessage> Messages { get; }
}
```

### Delete / Tombstone

```csharp
// Soft delete (stream can be recreated)
public Task<DeleteResult> DeleteAsync(
    string streamName,
    StreamState expectedState,
    TimeSpan? deadline = null,
    UserCredentials? userCredentials = null,
    CancellationToken cancellationToken = default);

// Permanent delete (stream name permanently reserved)
public Task<DeleteResult> TombstoneAsync(
    string streamName,
    StreamState expectedState,
    TimeSpan? deadline = null,
    UserCredentials? userCredentials = null,
    CancellationToken cancellationToken = default);
```

### Stream Metadata

```csharp
public Task<StreamMetadataResult> GetStreamMetadataAsync(
    string streamName,
    TimeSpan? deadline = null,
    UserCredentials? userCredentials = null,
    CancellationToken cancellationToken = default);

public Task<IWriteResult> SetStreamMetadataAsync(
    string streamName,
    StreamState expectedState,
    StreamMetadata metadata,
    Action<KurrentDBClientOperationOptions>? configureOperationOptions = null,
    TimeSpan? deadline = null,
    UserCredentials? userCredentials = null,
    CancellationToken cancellationToken = default);
```

## StreamMessage Types

```csharp
public abstract record StreamMessage {
    public record Event(ResolvedEvent ResolvedEvent) : StreamMessage;
    public record NotFound : StreamMessage;
    public record Ok : StreamMessage;
    public record FirstStreamPosition(StreamPosition StreamPosition) : StreamMessage;
    public record LastStreamPosition(StreamPosition StreamPosition) : StreamMessage;
    public record LastAllStreamPosition(Position Position) : StreamMessage;
    public record SubscriptionConfirmation(string SubscriptionId) : StreamMessage;
    public record AllStreamCheckpointReached(Position Position) : StreamMessage;
    public record StreamCheckpointReached(StreamPosition StreamPosition) : StreamMessage;
    public record CaughtUp : StreamMessage;
    public record FellBehind : StreamMessage;
    public record Unknown : StreamMessage;
}
```

## StreamMetadata & StreamAcl

```csharp
public class StreamMetadata {
    public StreamMetadata(
        int? maxCount = null,
        TimeSpan? maxAge = null,
        StreamPosition? truncateBefore = null,
        TimeSpan? cacheControl = null,
        StreamAcl? acl = null,
        JsonDocument? customMetadata = null);

    public int? MaxCount { get; }
    public TimeSpan? MaxAge { get; }
    public StreamPosition? TruncateBefore { get; }
    public TimeSpan? CacheControl { get; }
    public StreamAcl? Acl { get; }
    public JsonDocument? CustomMetadata { get; }
}

public class StreamAcl {
    // Single-role constructor
    public StreamAcl(
        string? readRole = null,
        string? writeRole = null,
        string? deleteRole = null,
        string? metaReadRole = null,
        string? metaWriteRole = null);

    // Multi-role constructor
    public StreamAcl(
        string[]? readRoles = null,
        string[]? writeRoles = null,
        string[]? deleteRoles = null,
        string[]? metaReadRoles = null,
        string[]? metaWriteRoles = null);

    public string[]? ReadRoles { get; }
    public string[]? WriteRoles { get; }
    public string[]? DeleteRoles { get; }
    public string[]? MetaReadRoles { get; }
    public string[]? MetaWriteRoles { get; }
}
```

## Filter Types

### EventTypeFilter

```csharp
public class EventTypeFilter {
    public static readonly EventTypeFilter None;

    public static IEventFilter ExcludeSystemEvents(uint maxSearchWindow = 32);
    public static IEventFilter Prefix(string prefix);
    public static IEventFilter Prefix(params string[] prefixes);
    public static IEventFilter Prefix(uint maxSearchWindow, params string[] prefixes);
    public static IEventFilter RegularExpression(string regex, uint maxSearchWindow = 32);
    public static IEventFilter RegularExpression(Regex regex, uint maxSearchWindow = 32);
}
```

### StreamFilter

```csharp
public class StreamFilter {
    public static readonly StreamFilter None;

    public static IEventFilter Prefix(string prefix);
    public static IEventFilter Prefix(params string[] prefixes);
    public static IEventFilter Prefix(uint maxSearchWindow, params string[] prefixes);
    public static IEventFilter RegularExpression(string regex, uint maxSearchWindow = 32);
    public static IEventFilter RegularExpression(Regex regex, uint maxSearchWindow = 32);
}
```

### SubscriptionFilterOptions

```csharp
public class SubscriptionFilterOptions {
    public SubscriptionFilterOptions(
        IEventFilter filter,
        uint checkpointInterval = 1,
        Func<StreamSubscription, Position, CancellationToken, Task>? checkpointReached = null);

    public IEventFilter Filter { get; }
    public uint CheckpointInterval { get; }
    public Func<StreamSubscription, Position, CancellationToken, Task> CheckpointReached { get; }
}
```

## KurrentDBPersistentSubscriptionsClient

```csharp
public class KurrentDBPersistentSubscriptionsClient : KurrentDBClientBase {
    // Create
    public Task CreateAsync(string streamName, string groupName,
        PersistentSubscriptionSettings settings,
        TimeSpan? deadline = null, UserCredentials? userCredentials = null,
        CancellationToken cancellationToken = default);

    public Task CreateToAllAsync(string groupName,
        PersistentSubscriptionSettings settings,
        TimeSpan? deadline = null, UserCredentials? userCredentials = null,
        CancellationToken cancellationToken = default);

    // Update
    public Task UpdateAsync(string streamName, string groupName,
        PersistentSubscriptionSettings settings,
        TimeSpan? deadline = null, UserCredentials? userCredentials = null,
        CancellationToken cancellationToken = default);

    // Subscribe (IAsyncEnumerable-based)
    public PersistentSubscriptionResult SubscribeToStream(string streamName, string groupName,
        int bufferSize = 10, UserCredentials? userCredentials = null,
        CancellationToken cancellationToken = default);

    // Subscribe (callback-based)
    public Task<PersistentSubscription> SubscribeToStreamAsync(string streamName, string groupName,
        Func<PersistentSubscription, ResolvedEvent, int?, CancellationToken, Task> eventAppeared,
        Action<PersistentSubscription, SubscriptionDroppedReason, Exception?>? subscriptionDropped = null,
        UserCredentials? userCredentials = null, int bufferSize = 10,
        CancellationToken cancellationToken = default);

    // Delete
    public Task DeleteAsync(string streamName, string groupName,
        TimeSpan? deadline = null, UserCredentials? userCredentials = null,
        CancellationToken cancellationToken = default);

    // List
    public IAsyncEnumerable<PersistentSubscriptionInfo> ListAsync(
        TimeSpan? deadline = null, UserCredentials? userCredentials = null,
        CancellationToken cancellationToken = default);

    // Info
    public Task<PersistentSubscriptionInfo> GetInfoAsync(string streamName, string groupName,
        TimeSpan? deadline = null, UserCredentials? userCredentials = null,
        CancellationToken cancellationToken = default);

    // Replay parked messages
    public Task ReplayParkedAsync(string streamName, string groupName,
        TimeSpan? deadline = null, UserCredentials? userCredentials = null,
        CancellationToken cancellationToken = default);

    // Restart subsystem
    public Task RestartSubsystemAsync(
        TimeSpan? deadline = null, UserCredentials? userCredentials = null,
        CancellationToken cancellationToken = default);
}
```

### PersistentSubscriptionSettings

```csharp
public class PersistentSubscriptionSettings {
    public PersistentSubscriptionSettings(
        bool resolveLinkTos = false,
        IPosition? startFrom = null,
        bool extraStatistics = false,
        TimeSpan? messageTimeout = null,       // default: 30s
        int maxRetryCount = 10,
        int liveBufferSize = 500,
        int readBatchSize = 20,
        int historyBufferSize = 500,
        TimeSpan? checkPointAfter = null,      // default: 2s
        int checkPointLowerBound = 10,
        int checkPointUpperBound = 1000,
        int maxSubscriberCount = 0,            // 0 = unlimited
        string consumerStrategyName = SystemConsumerStrategies.RoundRobin);

    public readonly bool ResolveLinkTos;
    public readonly IPosition? StartFrom;
    public readonly bool ExtraStatistics;
    public readonly TimeSpan MessageTimeout;
    public readonly int MaxRetryCount;
    public readonly int LiveBufferSize;
    public readonly int ReadBatchSize;
    public readonly int HistoryBufferSize;
    public readonly TimeSpan CheckPointAfter;
    public readonly int CheckPointLowerBound;
    public readonly int CheckPointUpperBound;
    public readonly int MaxSubscriberCount;
    public readonly string ConsumerStrategyName;
}
```

### Consumer Strategies

```csharp
public static class SystemConsumerStrategies {
    public const string RoundRobin = nameof(RoundRobin);
    public const string DispatchToSingle = nameof(DispatchToSingle);
    public const string Pinned = nameof(Pinned);
}
```

## KurrentDBProjectionManagementClient

```csharp
public class KurrentDBProjectionManagementClient : KurrentDBClientBase {
    public Task CreateOneTimeAsync(string query, ...);
    public Task CreateContinuousAsync(string name, string query,
        bool trackEmittedStreams = false, ...);
    public Task CreateTransientAsync(string name, string query, ...);
    public Task UpdateAsync(string name, string query,
        bool? trackEmittedStreams = null, ...);
    public Task DeleteAsync(string name, ...);
    public Task<string> GetStateAsync(string name, ...);
    public Task<ProjectionDetails> GetStatusAsync(string name, ...);
    public IAsyncEnumerable<ProjectionDetails> ListAsync(...);
    public Task EnableAsync(string name, ...);
    public Task DisableAsync(string name, ...);
    public Task ResetAsync(string name, ...);
}
```

## KurrentDBOperationsClient

```csharp
public class KurrentDBOperationsClient : KurrentDBClientBase {
    public Task<DatabaseScavengeResult> StartScavengeAsync(
        int threadCount = 1,
        int startFromChunk = 0,
        TimeSpan? deadline = null,
        UserCredentials? userCredentials = null,
        CancellationToken cancellationToken = default);

    public Task<DatabaseScavengeResult> StopScavengeAsync(
        string scavengeId,
        TimeSpan? deadline = null,
        UserCredentials? userCredentials = null,
        CancellationToken cancellationToken = default);
}
```

## KurrentDBUserManagementClient

```csharp
public class KurrentDBUserManagementClient : KurrentDBClientBase {
    public Task CreateUserAsync(string loginName, string fullName,
        string[] groups, string password, ...);
    public Task<UserDetails> GetUserAsync(string loginName, ...);
    public Task DeleteUserAsync(string loginName, ...);
    public Task EnableUserAsync(string loginName, ...);
    public Task DisableUserAsync(string loginName, ...);
    public IAsyncEnumerable<UserDetails> ListAllAsync(...);
    public Task ChangePasswordAsync(string loginName,
        string currentPassword, string newPassword, ...);
    public Task ResetPasswordAsync(string loginName, string newPassword, ...);
}
```

## DI Extensions

All in the `Microsoft.Extensions.DependencyInjection` namespace:

```csharp
public static class KurrentDBClientServiceCollectionExtensions {
    // From URI
    public static IServiceCollection AddKurrentDBClient(
        this IServiceCollection services, Uri address,
        Func<HttpMessageHandler>? createHttpMessageHandler = null);

    // From URI factory
    public static IServiceCollection AddKurrentDBClient(
        this IServiceCollection services, Func<IServiceProvider, Uri> addressFactory,
        Func<HttpMessageHandler>? createHttpMessageHandler = null);

    // From settings callback
    public static IServiceCollection AddKurrentDBClient(
        this IServiceCollection services,
        Action<KurrentDBClientSettings>? configureSettings = null);

    // From settings callback with IServiceProvider
    public static IServiceCollection AddKurrentDBClient(
        this IServiceCollection services,
        Func<IServiceProvider, Action<KurrentDBClientSettings>> configureSettings);

    // From connection string
    public static IServiceCollection AddKurrentDBClient(
        this IServiceCollection services, string connectionString,
        Action<KurrentDBClientSettings>? configureSettings = null);

    // From connection string factory
    public static IServiceCollection AddKurrentDBClient(
        this IServiceCollection services,
        Func<IServiceProvider, string> connectionStringFactory,
        Action<KurrentDBClientSettings>? configureSettings = null);
}
```

## OpenTelemetry

```csharp
namespace KurrentDB.Client.Extensions.OpenTelemetry {
    public static class TracerProviderBuilderExtensions {
        public static TracerProviderBuilder AddKurrentDBClientInstrumentation(
            this TracerProviderBuilder builder);
    }
}
```

## ServerCapabilities

```csharp
public record ServerCapabilities(
    bool SupportsBatchAppend = false,
    bool SupportsPersistentSubscriptionsToAll = false,
    bool SupportsPersistentSubscriptionsGetInfo = false,
    bool SupportsPersistentSubscriptionsRestartSubsystem = false,
    bool SupportsPersistentSubscriptionsReplayParked = false,
    bool SupportsMultiStreamAppend = false,
    bool SupportsPersistentSubscriptionsList = false
);
```

## Exception Types

### Stream Exceptions

| Exception | Cause |
|-----------|-------|
| `WrongExpectedVersionException` | Optimistic concurrency check failed. The stream is not at the expected revision. Includes `ExpectedStreamState` and `ActualStreamState` properties. |
| `StreamNotFoundException` | Attempted to read or subscribe to a stream that does not exist. |
| `StreamDeletedException` | Stream was soft-deleted. It can be recreated by appending new events. |
| `StreamTombstonedException` | Stream was permanently deleted (tombstoned). The stream name can never be reused. |
| `AppendRecordSizeExceededException` | Individual event exceeds the server's maximum record size. |
| `AppendTransactionMaxSizeExceededException` | Total append transaction exceeds server's maximum transaction size. |

### Auth Exceptions

| Exception | Cause |
|-----------|-------|
| `AccessDeniedException` | Authenticated user lacks permission for the operation. |
| `NotAuthenticatedException` | Credentials are invalid or missing. |

### Connection Exceptions

| Exception | Cause |
|-----------|-------|
| `DiscoveryException` | Failed to discover cluster nodes via gossip after all retry attempts. |
| `NotLeaderException` | Operation was sent to a non-leader node. The client auto-redirects. |
| `ConnectionStringParseException` | Base class for connection string parsing errors. |
| `NoSchemeException` | Connection string missing scheme prefix. |
| `InvalidSchemeException` | Unrecognized connection string scheme. |
| `InvalidHostException` | Malformed host in connection string. |
| `InvalidUserCredentialsException` | Malformed credentials in connection string. |
| `InvalidKeyValuePairException` | Malformed query parameter. |
| `DuplicateKeyException` | Duplicate query parameter key. |
| `InvalidSettingException` | Invalid value for a known parameter. |
| `InvalidClientCertificateException` | Client certificate file is invalid or not found. |

### Persistent Subscription Exceptions

| Exception | Cause |
|-----------|-------|
| `PersistentSubscriptionNotFoundException` | Named subscription group does not exist. |
| `MaximumSubscribersReachedException` | Consumer group has reached `MaxSubscriberCount`. |
| `PersistentSubscriptionDroppedByServerException` | Server dropped the persistent subscription connection. |

### Other Exceptions

| Exception | Cause |
|-----------|-------|
| `ScavengeNotFoundException` | Scavenge operation with the given ID not found. |
| `UserNotFoundException` | User with the given login name not found. |
| `RequiredMetadataPropertyMissingException` | Required metadata property is missing from event metadata. |

## Write Result Types

```csharp
public interface IWriteResult { }

public class SuccessResult : IWriteResult {
    public StreamState NextExpectedStreamState { get; }
    public Position LogPosition { get; }
}

public class WrongExpectedVersionResult : IWriteResult {
    public string StreamName { get; }
    public StreamState ExpectedStreamState { get; }
    public StreamState ActualStreamState { get; }
}

public class DeleteResult {
    public Position LogPosition { get; }
}
```

## Enums

```csharp
public enum Direction { Backwards, Forwards }
public enum ReadState { StreamNotFound, Ok }
public enum NodePreference { Leader, Follower, Random, ReadOnlyReplica }
public enum SubscriptionDroppedReason { Disposed, SubscriberError, ServerError }
```

## Version Compatibility

| Feature | Minimum Server Version |
|---------|----------------------|
| Core operations (append, read, subscribe) | 20.6.1 |
| Persistent subscriptions to $all | 21.10.0 |
| Server-side filtering | 21.6.0 |
| Multi-stream append | 25.1.0 |
| User certificate authentication | 24.6.0 |
