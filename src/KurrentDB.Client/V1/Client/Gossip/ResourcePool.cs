using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace KurrentDB.Client;

/// <summary>
/// Delegate for creating resources in a ResourcePool.
/// </summary>
/// <typeparam name="TConfig">The configuration type required to create the resource.</typeparam>
/// <typeparam name="TResource">The type of resource being created.</typeparam>
/// <param name="config">Configuration needed to create the resource.</param>
/// <param name="cancellation">Token to cancel the creation process.</param>
/// <returns>The created resource.</returns>
public delegate ValueTask<TResource> ResourceFactory<in TConfig, TResource>(TConfig config, CancellationToken cancellation);

/// <summary>
/// A pool that efficiently shares a resource among multiple consumers.
/// The shared resource is created only when needed and automatically regenerated if broken.
/// </summary>
/// <typeparam name="TConfig">The configuration type required to create the resource.</typeparam>
/// <typeparam name="TResource">The type of the shared resource.</typeparam>
public partial class ResourcePool<TConfig, TResource> : IAsyncDisposable {
	readonly CancellationTokenSource             _cts = new();
	readonly ResourceFactory<TConfig, TResource> _factory;
	readonly ILogger                             _logger;
	readonly string                              _resourceName;
	readonly TimeSpan                            _retryDelay;
	readonly SemaphoreSlim                       _semaphore = new(1, 1);

	TConfig          _config;
	Task<TResource>? _currentTask;
	bool             _disposed;

	/// <summary>
	/// Creates a new resource pool.
	/// </summary>
	/// <param name="factory">Factory function to create resources.</param>
	/// <param name="config">Configuration for creating resources.</param>
	/// <param name="retryDelay">Delay before propagating factory failure.</param>
	/// <param name="logger">Optional logger for events and errors.</param>
	/// <param name="resourceName">Optional name for the resource in logs. Defaults to the resource type name.</param>
	public ResourcePool(
		ResourceFactory<TConfig, TResource> factory,
		TConfig config,
		TimeSpan retryDelay,
		ILogger? logger = null,
		string? resourceName = null
	) {
		_factory      = factory ?? throw new ArgumentNullException(nameof(factory));
		_config       = config;
		_retryDelay   = retryDelay;
		_logger       = logger ?? NullLogger.Instance;
		_resourceName = resourceName ?? typeof(TResource).Name;

		LogPoolInitialized(_resourceName, retryDelay);

		// // Start creating the resource immediately
		// _currentTask = CreateResourceAsync(CancellationToken.None);
	}

	/// <summary>
	/// Asynchronously releases resources.
	/// </summary>
	public async ValueTask DisposeAsync() {
		if (_disposed)
			return;

		await _semaphore.WaitAsync().ConfigureAwait(false);
		LogSemaphoreAcquired(_resourceName);

		try {
			if (_disposed)
				return;

			_disposed = true;
			_cts.Cancel();
			_cts.Dispose();

			// Dispose resource if applicable
			if (_currentTask?.Status == TaskStatus.RanToCompletion) {
				var resource = _currentTask.Result;
				if (resource is IAsyncDisposable asyncDisposable)
					await asyncDisposable.DisposeAsync().ConfigureAwait(false);
				else if (resource is IDisposable disposable)
					disposable.Dispose();
			}

			_semaphore.Dispose();

			LogResourcePoolDisposed(_resourceName);
		}
		finally {
			_semaphore.Release();
			// Note: not logging semaphore release here since it's being disposed
		}
	}

	/// <summary>
	/// Gets the current shared resource, creating it if needed.
	/// </summary>
	/// <param name="cancellation">Token to cancel the operation.</param>
	/// <returns>The shared resource.</returns>
	public ValueTask<TResource> GetAsync(CancellationToken cancellation = default) {
		if (_disposed)
			throw new ObjectDisposedException(nameof(ResourcePool<,>));

		// Fast path if resource already exists
		var isCached = _currentTask is { Status: TaskStatus.RanToCompletion };
		LogResourceAccess(_resourceName, isCached);

		return isCached
			? new ValueTask<TResource>(_currentTask!.Result)
			: GetResourceAsync(cancellation);
	}

	/// <summary>
	/// Forces the pool to create a new instance of the resource.
	/// This should be called when the current resource is detected to be broken.
	/// </summary>
	/// <param name="cancellation">Token to cancel the operation.</param>
	/// <returns>A task that completes when the reset is done.</returns>
	public async Task ResetAsync(CancellationToken cancellation = default) {
		if (_disposed)
			throw new ObjectDisposedException(nameof(ResourcePool<,>));

		await _semaphore.WaitAsync(cancellation).ConfigureAwait(false);
		LogSemaphoreAcquired(_resourceName);

		try {
			LogResourceReset(_resourceName);
			_currentTask = CreateResourceAsync(cancellation);
		}
		finally {
			_semaphore.Release();
			LogSemaphoreReleased(_resourceName);
		}
	}

	public async Task ResetWithConfigAsync(TConfig newConfig, CancellationToken cancellation = default) {
		if (_disposed)
			throw new ObjectDisposedException(nameof(ResourcePool<,>));

		await _semaphore.WaitAsync(cancellation).ConfigureAwait(false);
		LogSemaphoreAcquired(_resourceName);

		try {
			LogResourceReset(_resourceName);
			_config      = newConfig; // Update the configuration
			_currentTask = CreateResourceAsync(cancellation);
		}
		finally {
			_semaphore.Release();
			LogSemaphoreReleased(_resourceName);
		}
	}

	async ValueTask<TResource> GetResourceAsync(CancellationToken cancellation) {
		using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, cancellation);

		var linkedToken = linkedCts.Token;

		await _semaphore.WaitAsync(linkedToken).ConfigureAwait(false);

		LogSemaphoreAcquired(_resourceName);

		try {
			// Check again inside lock if task already exists and is successful
			if (_currentTask is { Status: TaskStatus.RanToCompletion })
				return _currentTask.Result;

			// Create a new task if none exists (first access) or previous one failed
			if (_currentTask == null || _currentTask.IsFaulted)
				_currentTask = CreateResourceAsync(linkedToken);
			else
				LogWaitingForResource(_resourceName);

			// Await and return the result
			var result = await _currentTask.ConfigureAwait(false);
			LogResourceReturned(_resourceName);
			return result;
		}
		finally {
			_semaphore.Release();
			LogSemaphoreReleased(_resourceName);
		}
	}

	async Task<TResource> CreateResourceAsync(CancellationToken cancellation) {
		try {
			LogCreatingResource(_resourceName);

			var result = await _factory(_config, cancellation)
				.ConfigureAwait(false);

			LogResourceCreated(_resourceName);
			return result;
		}
		catch (OperationCanceledException) when (cancellation.IsCancellationRequested) {
			LogResourceCreationCanceled(_resourceName);
			throw;
		}
		catch (Exception ex) {
			// Log error, delay, then propagate failure
			LogResourceCreationFailed(ex, _resourceName);

			// Add a small delay to avoid tight retry loops
			await Task.Delay(_retryDelay, CancellationToken.None).ConfigureAwait(false);

			// Propagate the exception
			throw;
		}
	}
}

// Source-generated logging methods
public partial class ResourcePool<TConfig, TResource> {
	// Core resource lifecycle events
	[LoggerMessage(Level = LogLevel.Debug, Message = "{ResourceName} explicitly reset")]
	private partial void LogResourceReset(string resourceName);

	[LoggerMessage(Level = LogLevel.Debug, Message = "{ResourceName} pool disposed")]
	private partial void LogResourcePoolDisposed(string resourceName);

	[LoggerMessage(Level = LogLevel.Debug, Message = "Creating {ResourceName}")]
	private partial void LogCreatingResource(string resourceName);

	[LoggerMessage(Level = LogLevel.Debug, Message = "{ResourceName} created successfully")]
	private partial void LogResourceCreated(string resourceName);

	[LoggerMessage(Level = LogLevel.Debug, Message = "{ResourceName} creation canceled")]
	private partial void LogResourceCreationCanceled(string resourceName);

	[LoggerMessage(Level = LogLevel.Debug, Message = "{ResourceName} reported as broken")]
	private partial void LogResourceBroken(string resourceName);

	[LoggerMessage(Level = LogLevel.Error, Message = "Failed to create {ResourceName}")]
	private partial void LogResourceCreationFailed(Exception ex, string resourceName);

	// Detailed state tracking
	[LoggerMessage(Level = LogLevel.Trace, Message = "Getting {ResourceName} (cached: {IsCached})")]
	private partial void LogResourceAccess(string resourceName, bool isCached);

	[LoggerMessage(Level = LogLevel.Debug, Message = "Waiting for {ResourceName} creation to complete")]
	private partial void LogWaitingForResource(string resourceName);

	[LoggerMessage(Level = LogLevel.Trace, Message = "{ResourceName} pool initialized with retry delay: {RetryDelay}")]
	private partial void LogPoolInitialized(string resourceName, TimeSpan retryDelay);

	[LoggerMessage(Level = LogLevel.Debug, Message = "Returning {ResourceName} after waiting for creation")]
	private partial void LogResourceReturned(string resourceName);

	[LoggerMessage(Level = LogLevel.Debug, Message = "Semaphore acquired for {ResourceName}")]
	private partial void LogSemaphoreAcquired(string resourceName);

	[LoggerMessage(Level = LogLevel.Debug, Message = "Semaphore released for {ResourceName}")]
	private partial void LogSemaphoreReleased(string resourceName);
}

public static class ResourcePoolExtensions {
	static readonly ConditionalWeakTable<object, PoolReference> Resources = new();

	/// <summary>
	/// Attempts to execute an operation with the resource from the pool.
	/// Automatically resets the pool and retries if the operation fails with a transient error.
	/// </summary>
	public static async Task<TResult> ExecuteWithRetryAsync<TConfig, TResource, TResult>(
		this ResourcePool<TConfig, TResource> pool,
		Func<TResource, Task<TResult>> operation,
		Func<Exception, bool> isTransientError,
		int maxRetries = 3,
		CancellationToken cancellation = default
	) {
		var attempts = 0;

		while (true) {
			attempts++;

			try {
				var resource = await pool.GetAsync(cancellation);
				return await operation(resource);
			}
			catch (Exception ex) when (attempts <= maxRetries && isTransientError(ex)) {
				// Reset the pool and retry
				await pool.ResetAsync(cancellation);

				// Optional backoff delay
				await Task.Delay(TimeSpan.FromMilliseconds(Math.Pow(2, attempts) * 100), cancellation);
			}
		}
	}

	public static void RegisterPool<TConfig, TResource>(this TResource resource, ResourcePool<TConfig, TResource> pool) {
		if (resource is null)
			throw new ArgumentNullException(nameof(resource));

		Resources.Add(resource, new PoolReference(pool.ResetAsync));
	}

	public static async Task ResetPoolAsync<T>(this T resource, CancellationToken cancellation = default) where T : class {
		if (Resources.TryGetValue(resource, out var poolRef)) await poolRef.Reset(cancellation);
	}

	record PoolReference(Func<CancellationToken, Task> Reset);
}
