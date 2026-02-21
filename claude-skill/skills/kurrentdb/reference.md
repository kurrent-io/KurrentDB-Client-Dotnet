# KurrentDB .NET Client SDK — API Reference

Comprehensive reference for the `KurrentDB.Client` NuGet package.

## Connection String

### Format

```
scheme://[user:password@]host[:port][,host[:port]...][?key=value[&key=value...]]
```

Default port is `2113`. Parameters are case-insensitive. Duplicate keys are not allowed and will throw `DuplicateKeyException`.

### Supported Schemes

All schemes are functionally equivalent — they differ only in name:

| Scheme | Discovery Variant | Notes |
|--------|------------------|-------|
| `esdb` | `esdb+discover` | Original EventStoreDB scheme, still fully supported |
| `kdb` | `kdb+discover` | Short alias |
| `kurrent` | `kurrent+discover` | Alias |
| `kurrentdb` | `kurrentdb+discover` | Canonical KurrentDB scheme |

Without `+discover`, the client connects directly to the specified node(s). With `+discover`, the client uses gossip-based discovery to find cluster members — required for clusters, optional for single nodes.

**Single node vs cluster:** When one host is provided without `+discover`, the client treats it as a single-node direct connection. When multiple hosts are provided, or `+discover` is used, hosts are treated as gossip seeds for cluster discovery.

### Parameters

#### TLS & Security

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `tls` | bool | `true` | Enable TLS encryption. Set to `false` for insecure (unencrypted) connections. Maps to `ConnectivitySettings.Insecure` (inverted). |
| `tlsVerifyCert` | bool | `true` | Verify the server's TLS certificate chain. Set to `false` to accept self-signed certificates (development only). Maps to `ConnectivitySettings.TlsVerifyCert`. |
| `tlsCaFile` | string | — | Absolute or relative path to a CA certificate file (PEM or DER). Used to trust a private CA. Throws `InvalidClientCertificateException` if the file doesn't exist or has an invalid format. Maps to `ConnectivitySettings.TlsCaFile`. |
| `userCertFile` | string | — | Path to client certificate PEM file for certificate-based authentication (server 24.6+). **Must** be paired with `userKeyFile`. Maps to `ConnectivitySettings.ClientCertificate`. |
| `userKeyFile` | string | — | Path to client private key PEM file. **Must** be paired with `userCertFile`. Throws `InvalidClientCertificateException` if only one is set or files don't exist. |

#### Timeouts & Keep-Alive

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `defaultDeadline` | int | `10000` | Default timeout for all operations, in milliseconds. Maps to `DefaultDeadline`. |
| `keepAliveInterval` | int | `10000` | gRPC keep-alive ping interval in milliseconds. Use `-1` for infinite (disable pings). Must be `>= 0` or `-1`. Maps to `ConnectivitySettings.KeepAliveInterval`. |
| `keepAliveTimeout` | int | `10000` | gRPC keep-alive ping timeout in milliseconds. If a ping response is not received within this time, the connection is considered dead. Use `-1` for infinite. Must be `>= 0` or `-1`. Maps to `ConnectivitySettings.KeepAliveTimeout`. |

#### Cluster Discovery

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `nodePreference` | string | `leader` | Preferred node type for operations. Values: `leader` (write-capable node), `follower` (read-optimized, redirects writes), `random` (any node), `readonlyreplica` (read-only replica node). Maps to `ConnectivitySettings.NodePreference`. |
| `maxDiscoverAttempts` | int | `10` | Maximum number of gossip discovery attempts before throwing `DiscoveryException`. Maps to `ConnectivitySettings.MaxDiscoverAttempts`. |
| `discoveryInterval` | int | `100` | Interval between discovery attempts, in milliseconds. Maps to `ConnectivitySettings.DiscoveryInterval`. |
| `gossipTimeout` | int | `5000` | Timeout for a single gossip request, in milliseconds. Maps to `ConnectivitySettings.GossipTimeout`. |

#### Client Behavior

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `connectionName` | string | — | A name identifying this client connection in server logs and diagnostics. Maps to `ConnectionName`. |
| `throwOnAppendFailure` | bool | `true` | When `true`, `AppendToStreamAsync` throws `WrongExpectedVersionException` on concurrency conflicts. When `false`, it returns a `WrongExpectedVersionResult` instead, allowing non-exception control flow. Maps to `OperationOptions.ThrowOnAppendFailure`. |

### Examples

```
# Insecure local development
kurrentdb://localhost:2113?tls=false

# With credentials, TLS enabled, skip cert verification
kurrentdb://admin:changeit@localhost:2113?tls=true&tlsVerifyCert=false

# Cluster discovery with follower preference
kurrentdb+discover://admin:changeit@node1:2113,node2:2113,node3:2113?nodePreference=follower&maxDiscoverAttempts=5

# Custom CA certificate
kurrentdb://admin:changeit@localhost:2113?tls=true&tlsCaFile=/path/to/ca.crt

# Client certificate authentication (no user:pass needed)
kurrentdb://localhost:2113?tls=true&userCertFile=/path/to/user.crt&userKeyFile=/path/to/user.key

# Named connection with custom timeouts
kurrentdb://admin:changeit@localhost:2113?connectionName=order-service&defaultDeadline=30000&keepAliveInterval=5000

# Non-throwing append failures
kurrentdb://admin:changeit@localhost:2113?throwOnAppendFailure=false
```

### Parsing Errors

| Exception | Cause |
|-----------|-------|
| `NoSchemeException` | Connection string has no `://` separator |
| `InvalidSchemeException` | Scheme is not one of the 8 supported values |
| `InvalidHostException` | Malformed host (empty, non-numeric port) |
| `InvalidUserCredentialsException` | User info is not in `user:password` format |
| `InvalidKeyValuePairException` | Query parameter is not in `key=value` format |
| `DuplicateKeyException` | Same parameter key appears more than once |
| `InvalidSettingException` | Unknown parameter name, or value has wrong type (e.g., non-integer for `maxDiscoverAttempts`) |
| `InvalidClientCertificateException` | Certificate/key file not found, invalid format, or only one of `userCertFile`/`userKeyFile` provided |

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
