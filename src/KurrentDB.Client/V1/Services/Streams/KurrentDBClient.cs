using System.Text.Json;
using System.Threading.Channels;
using EventStore.Client;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using static EventStore.Client.Streams.ReadReq.Types.Options.Types;

namespace KurrentDB.Client;

// EventStoreClient

/// <summary>
/// The client used for operations on streams.
/// </summary>
public sealed partial class KurrentDBClient : KurrentDBClientBase {
	static readonly JsonSerializerOptions StreamMetadataJsonSerializerOptions = new() {
		Converters = { StreamMetadataJsonConverter.Instance },
	};

	static readonly BoundedChannelOptions ReadBoundedChannelOptions = new(capacity: 1) {
		SingleReader                  = true,
		SingleWriter                  = true,
		AllowSynchronousContinuations = true
	};

	static readonly Dictionary<string, Func<RpcException, Exception>> ExceptionMap = new() {
		[Constants.Exceptions.InvalidTransaction] = ex => new InvalidTransactionException(ex.Message, ex),
		[Constants.Exceptions.StreamDeleted] = ex => new StreamDeletedException(
			ex.Trailers.FirstOrDefault(x => x.Key == Constants.Exceptions.StreamName)?.Value ?? "<unknown>",
			ex
		),
		[Constants.Exceptions.WrongExpectedVersion] = ex => new WrongExpectedVersionException(
			ex.Trailers.FirstOrDefault(x => x.Key == Constants.Exceptions.StreamName)?.Value!,
			ex.Trailers.GetStreamState(Constants.Exceptions.ExpectedVersion),
			ex.Trailers.GetStreamState(Constants.Exceptions.ActualVersion),
			ex,
			ex.Message
		),
		[Constants.Exceptions.MaximumAppendSizeExceeded] = ex => new MaximumAppendSizeExceededException(
			ex.Trailers.GetIntValueOrDefault(Constants.Exceptions.MaximumAppendSize),
			ex
		),
		[Constants.Exceptions.StreamNotFound] = ex => new StreamNotFoundException(
			ex.Trailers.FirstOrDefault(x => x.Key == Constants.Exceptions.StreamName)?.Value!,
			ex
		),
		[Constants.Exceptions.MissingRequiredMetadataProperty] = ex => new RequiredMetadataPropertyMissingException(
			ex.Trailers.FirstOrDefault(x => x.Key == Constants.Exceptions.MissingRequiredMetadataProperty)?.Value!,
			ex
		),
	};

	readonly ILogger<KurrentDBClient> _log;
	readonly CancellationTokenSource  _disposedTokenSource;

	Lazy<StreamAppender> _batchAppenderLazy;

	StreamAppender BatchAppender => _batchAppenderLazy.Value;

	/// <summary>
	/// Constructs a new <see cref="KurrentDBClient"/>. This is not intended to be called directly from your code.
	/// </summary>
	/// <param name="options"></param>
	public KurrentDBClient(IOptions<KurrentDBClientSettings> options) : this(options.Value) { }

	/// <summary>
	/// Constructs a new <see cref="KurrentDBClient"/>.
	/// </summary>
	/// <param name="settings"></param>
	public KurrentDBClient(KurrentDBClientSettings? settings = null) : base(settings, ExceptionMap) {
		_log                 = Settings.LoggerFactory.CreateLogger<KurrentDBClient>();
		_disposedTokenSource = new CancellationTokenSource();
		_batchAppenderLazy   = new Lazy<StreamAppender>(CreateStreamAppender);
	}

	void SwapStreamAppender(Exception ex) =>
		Interlocked.Exchange(ref _batchAppenderLazy, new Lazy<StreamAppender>(CreateStreamAppender)).Value.Dispose();

	// todo: might be nice to have two different kinds of appenders and we decide which to instantiate according to the server caps.
	StreamAppender CreateStreamAppender() => new StreamAppender(
		Settings,
		GetChannelInfo(_disposedTokenSource.Token),
		// new(ChannelInfo),
		_disposedTokenSource.Token,
		SwapStreamAppender
	);

	static FilterOptions? GetFilterOptions(IEventFilter? filter, uint checkpointInterval = 0) {
		if (filter == null
		 || filter.Equals(StreamFilter.None)
		 || filter.Equals(EventTypeFilter.None))
			return null;

		var options = filter switch {
			StreamFilter => new FilterOptions {
				StreamIdentifier = (filter.Prefixes, filter.Regex) switch {
					(_, _) when (filter.Prefixes?.Length ?? 0) == 0 && filter.Regex != RegularFilterExpression.None => new FilterOptions.Types.Expression { Regex = filter.Regex },
					(_, _) when (filter.Prefixes?.Length ?? 0) != 0 && filter.Regex == RegularFilterExpression.None => new FilterOptions.Types.Expression { Prefix = { Array.ConvertAll(filter.Prefixes!, e => e.ToString()) } },
					_ => throw new InvalidOperationException()
				}
			},
			EventTypeFilter => new FilterOptions {
				EventType = (filter.Prefixes, filter.Regex) switch {
					(_, _) when (filter.Prefixes?.Length ?? 0) == 0 && filter.Regex != RegularFilterExpression.None => new FilterOptions.Types.Expression { Regex = filter.Regex },
					(_, _) when (filter.Prefixes?.Length ?? 0) != 0 && filter.Regex == RegularFilterExpression.None => new FilterOptions.Types.Expression { Prefix = { Array.ConvertAll(filter.Prefixes!, e => e.ToString()) } },
					_ => throw new InvalidOperationException()
				}
			},
			_ => null
		};

		if (options is null)
			return null;

		if (filter.MaxSearchWindow.HasValue)
			options.Max = filter.MaxSearchWindow.Value;
		else
			options.Count = new Empty();

		options.CheckpointIntervalMultiplier = checkpointInterval;

		return options;
	}

	static FilterOptions? GetFilterOptions(SubscriptionFilterOptions? filterOptions) =>
		filterOptions == null ? null : GetFilterOptions(filterOptions.Filter, filterOptions.CheckpointInterval);

	static InvalidOperationException InvalidOption<T>(T option) where T : Enum =>
		new InvalidOperationException($"The {typeof(T).Name} {option:x} was not valid.");

	protected override async ValueTask DisposeAsyncCore() {
		if (_batchAppenderLazy.IsValueCreated)
			_batchAppenderLazy.Value.Dispose();

		_disposedTokenSource.Dispose();
		await DisposeAsync().ConfigureAwait(false);
	}
}
