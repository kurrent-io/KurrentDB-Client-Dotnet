using System.Net;
using Grpc.Core;
using Grpc.Net.Client.Balancer;
using KurrentDB.Client.LoadBalancer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace KurrentDb.Client;

interface ISystemClock {
	DateTime UtcNow { get; }
}

class SystemClock : ISystemClock {
	public DateTime UtcNow => DateTime.UtcNow;
}

// A convenience API for interacting with System.Threading.Timer in a way
// that doesn't capture the ExecutionContext. We should be using this (or equivalent)
// everywhere we use timers to avoid rooting any values stored in asynclocals.
static class NonCapturingTimer {
	public static Timer Create(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period) {
		// Don't capture the current ExecutionContext and its AsyncLocals onto the timer
		var restoreFlow = false;
		try {
			if (!ExecutionContext.IsFlowSuppressed()) {
				ExecutionContext.SuppressFlow();
				restoreFlow = true;
			}

			return new Timer(callback, state, dueTime, period);
		}
		finally {
			// Restore the current ExecutionContext
			if (restoreFlow) ExecutionContext.RestoreFlow();
		}
	}
}

partial class DnsMultiHostResolver : PollingResolver {
	public static readonly BalancerAttributesKey<string> ConnectionManagerHostOverrideKey = new("HostOverride");

	// To prevent excessive re-resolution, we enforce a rate limit on DNS resolution requests.
	static readonly TimeSpan MinimumDnsResolutionRate = TimeSpan.FromSeconds(15);

	readonly string   _dnsAddress;
	readonly ILogger  _logger;
	readonly Uri      _originalAddress;
	readonly int      _port;
	readonly TimeSpan _refreshInterval;

	/// Internal for testing.
	internal readonly ISystemClock SystemClock = new SystemClock();

	DateTime _lastResolveStart;
	Timer?   _timer;

	public DnsMultiHostResolver(Uri address, int defaultPort, ILoggerFactory loggerFactory, TimeSpan refreshInterval, IBackoffPolicyFactory backoffPolicyFactory) : base(
		loggerFactory,
		backoffPolicyFactory
	) {
		_originalAddress = address;

		// DNS address has the format: dns:[//authority/]host[:port]
		// Because the host is specified in the path, the port needs to be parsed manually
		var addressParsed = new Uri("temp://" + address.AbsolutePath.TrimStart('/'));

		_dnsAddress      = addressParsed.Host;
		_port            = addressParsed.Port == -1 ? defaultPort : addressParsed.Port;
		_refreshInterval = refreshInterval;
		_logger          = loggerFactory.CreateLogger(typeof(DnsMultiHostResolver));
	}

	protected override void OnStarted() {
		base.OnStarted();

		if (_refreshInterval != Timeout.InfiniteTimeSpan) {
			_timer = NonCapturingTimer.Create(OnTimerCallback, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
			_timer.Change(_refreshInterval, _refreshInterval);
		}
	}

	protected override async Task ResolveAsync(CancellationToken cancellationToken) {
		try {
			var elapsedTimeSinceLastRefresh = SystemClock.UtcNow - _lastResolveStart;
			if (elapsedTimeSinceLastRefresh < MinimumDnsResolutionRate) {
				var delay = MinimumDnsResolutionRate - elapsedTimeSinceLastRefresh;
				StartingRateLimitDelay(_logger, delay, MinimumDnsResolutionRate);

				await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
			}

			var lastResolveStart = SystemClock.UtcNow;

			if (string.IsNullOrEmpty(_dnsAddress))
				throw new InvalidOperationException($"Resolver address '{_originalAddress}' is not valid. Please use dns:/// for DNS provider.");

			StartingDnsQuery(_logger, _dnsAddress);
			var addresses =
#if NET6_0_OR_GREATER
				await Dns.GetHostAddressesAsync(_dnsAddress, cancellationToken).ConfigureAwait(false);
#else
                await Dns.GetHostAddressesAsync(_dnsAddress).ConfigureAwait(false);
#endif

			ReceivedDnsResults(_logger, addresses.Length, _dnsAddress, addresses);

			var hostOverride = $"{_dnsAddress}:{_port}";
			var endpoints = addresses.Select(a => {
					var address = new BalancerAddress(a.ToString(), _port);
					address.Attributes.Set(ConnectionManagerHostOverrideKey, hostOverride);
					return address;
				}
			).ToArray();

			var resolverResult = ResolverResult.ForResult(endpoints);
			Listener(resolverResult);

			// Only update last resolve start if successful. Backoff will handle limiting resolves on failure.
			_lastResolveStart = lastResolveStart;
		}
		catch (Exception ex) {
			var message = $"Error getting DNS hosts for address '{_dnsAddress}'.";

			ErrorQueryingDns(_logger, _dnsAddress, ex);
			//Listener(ResolverResult.ForFailure(GrpcProtocolHelpers.CreateStatusFromException(message, ex, StatusCode.Unavailable)));

			Listener(ResolverResult.ForFailure(new Status(StatusCode.Unavailable, message, ex)));
		}
	}

	protected override void Dispose(bool disposing) {
		base.Dispose(disposing);

		_timer?.Dispose();
	}

	void OnTimerCallback(object? state) {
		try {
			Refresh();
		}
		catch (Exception ex) {
			ErrorFromRefreshInterval(_logger, ex);
		}
	}
}

partial class DnsMultiHostResolver {
	[LoggerMessage(
		Level = LogLevel.Debug,
		EventId = 1,
		EventName = "StartingRateLimitDelay",
		Message = "Starting rate limit delay of {DelayDuration}. DNS resolution rate limit is once every {RateLimitDuration}."
	)]
	public static partial void StartingRateLimitDelay(ILogger logger, TimeSpan delayDuration, TimeSpan rateLimitDuration);

	[LoggerMessage(Level = LogLevel.Trace, EventId = 2, EventName = "StartingDnsQuery", Message = "Starting DNS query to get hosts from '{DnsAddress}'.")]
	public static partial void StartingDnsQuery(ILogger logger, string dnsAddress);

	[LoggerMessage(
		Level = LogLevel.Debug,
		EventId = 3,
		EventName = "ReceivedDnsResults",
		Message = "Received {ResultCount} DNS results from '{DnsAddress}'. Results: {DnsResults}"
	)]
	private static partial void ReceivedDnsResults(ILogger logger, int resultCount, string dnsAddress, string dnsResults);

	public static void ReceivedDnsResults(ILogger logger, int resultCount, string dnsAddress, IList<IPAddress> dnsResults) {
		if (logger.IsEnabled(LogLevel.Debug)) ReceivedDnsResults(logger, resultCount, dnsAddress, string.Join(", ", dnsResults));
	}

	[LoggerMessage(Level = LogLevel.Error, EventId = 4, EventName = "ErrorQueryingDns", Message = "Error querying DNS hosts for '{DnsAddress}'.")]
	public static partial void ErrorQueryingDns(ILogger logger, string dnsAddress, Exception ex);

	[LoggerMessage(Level = LogLevel.Error, EventId = 5, EventName = "ErrorFromRefreshIntervalTimer", Message = "Error from refresh interval timer.")]
	public static partial void ErrorFromRefreshInterval(ILogger logger, Exception ex);
}

/// <summary>
/// A <see cref="ResolverFactory"/> that matches the URI scheme <c>dns</c>
/// and creates <see cref="DnsResolver"/> instances.
/// <para>
/// Note: Experimental API that can change or be removed without any prior notice.
/// </para>
/// </summary>
public sealed class DnsMultiHostResolverFactory : ResolverFactory {
	readonly TimeSpan _refreshInterval;

	/// <summary>
	/// Initializes a new instance of the <see cref="DnsMultiHostResolverFactory"/> class with a refresh interval.
	/// </summary>
	/// <param name="refreshInterval">An interval for automatically refreshing the DNS hostname.</param>
	public DnsMultiHostResolverFactory(TimeSpan refreshInterval) => _refreshInterval = refreshInterval;

	/// <inheritdoc />
	public override string Name => "dns";

	/// <inheritdoc />
	public override Resolver Create(ResolverOptions options) {
		var channelOptions = options.ChannelOptions;

		var backoffPolicyFactory = channelOptions.ServiceProvider?.GetService<IBackoffPolicyFactory>()
		                        ?? new ExponentialBackoffPolicyFactory(channelOptions.InitialReconnectBackoff, channelOptions.MaxReconnectBackoff);

		return new DnsMultiHostResolver(options.Address, options.DefaultPort, options.LoggerFactory, _refreshInterval, backoffPolicyFactory);
	}
}
