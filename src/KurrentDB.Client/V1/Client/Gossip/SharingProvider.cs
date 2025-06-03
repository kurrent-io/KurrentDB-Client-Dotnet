using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace KurrentDB.Client;

class SharingProvider(ILogger logger) {
	protected ILogger Logger { get; } = logger;
}

// Given a factory for items of type TOutput, where the items:
//  - are expensive to produce
//  - can be shared by consumers
//  - can break
//  - can fail to be successfully produced by the factory to begin with.
//
// This class will make minimal use of the factory to provide items to consumers.
// The Factory can produce and return an item, or it can throw an exception.
// We pass the factory a OnBroken callback to be called later if that instance becomes broken.
//   the OnBroken callback can be called multiple times, the factory will be called once.
//   the argument to the OnBroken callback is the input to construct the next item.
//
// The factory will not be called multiple times concurrently so does not need to be
// thread safe, but it does need to terminate.
//
// This class is thread safe.

class SharingProvider<TInput, TOutput> : SharingProvider, IDisposable {
	readonly Func<TInput, Action<TInput>, Task<TOutput>> _factory;
	readonly TimeSpan                                    _factoryRetryDelay;
	readonly TInput                                      _initialInput;
	readonly Action<TOutput>                             _onRefresh;

	TaskCompletionSource<TOutput> _currentBox;
	bool                          _disposed;

	public SharingProvider(
		Func<TInput, Action<TInput>, Task<TOutput>> factory,
		TimeSpan factoryRetryDelay,
		TInput initialInput,
		Action<TOutput>? onRefresh = null,
		ILogger? logger = null
	) : base(logger ?? NullLogger<SharingProvider>.Instance) {
		_factory           = factory;
		_factoryRetryDelay = factoryRetryDelay;
		_initialInput      = initialInput;
		_onRefresh         = onRefresh ?? (_ => Logger.LogDebug("{type} refreshed!", typeof(TOutput).Name));
		_currentBox        = new(TaskCreationOptions.RunContinuationsAsynchronously);
		_                  = FillBoxAsync(_currentBox, input: initialInput);
	}

	public Task<TOutput> CurrentAsync => _currentBox.Task;

	//public void Reset() => OnBroken(_currentBox, _initialInput);

	public void Reset(TInput? input = default) => OnBroken(_currentBox, input ?? _initialInput);


	//public Task ResetAsync(TInput? input = default) => OnBrokenAsync(_currentBox, input ?? _initialInput);

	// async Task OnBrokenAsync(TaskCompletionSource<TOutput> brokenBox, TInput input) {
	// 	if (!brokenBox.Task.IsCompleted) {
	// 		// factory is still working on this box. don't create a new box to fill
	// 		// or we would have to require the factory be thread safe.
	// 		Logger.LogDebug("{type} returned to factory. Production already in progress.", typeof(TOutput).Name);
	// 		return;
	// 	}
	//
	// 	// replace _currentBox with a new one, but only if it is the broken one.
	// 	var originalBox = Interlocked.CompareExchange(
	// 		location1: ref _currentBox,
	// 		value: new(TaskCreationOptions.RunContinuationsAsynchronously),
	// 		comparand: brokenBox);
	//
	// 	if (originalBox == brokenBox) {
	// 		// replaced the _currentBox, call the factory to fill it.
	// 		Logger.LogDebug("{type} returned to factory. Producing a new one.", typeof(TOutput).Name);
	// 		await FillBoxAsync(_currentBox, input);
	// 	} else {
	// 		// did not replace. a new one was created previously. do nothing.
	// 		Logger.LogDebug("{type} returned to factory. Production already complete.", typeof(TOutput).Name);
	// 	}
	// }

	// Call this to return a box containing a defective item, or indeed no item at all.
	// A new box will be produced and filled if necessary.
	void OnBroken(TaskCompletionSource<TOutput> brokenBox, TInput input) {
		if (!brokenBox.Task.IsCompleted) {
			// factory is still working on this box. don't create a new box to fill
			// or we would have to require the factory be thread safe.
			Logger.LogDebug("{type} returned to factory. Production already in progress.", typeof(TOutput).Name);
			return;
		}

		// replace _currentBox with a new one, but only if it is the broken one.
		var originalBox = Interlocked.CompareExchange(
			location1: ref _currentBox,
			value: new(TaskCreationOptions.RunContinuationsAsynchronously),
			comparand: brokenBox);

		if (originalBox == brokenBox) {
			// replaced the _currentBox, call the factory to fill it.
			Logger.LogDebug("{type} returned to factory. Producing a new one.", typeof(TOutput).Name);
			_ = FillBoxAsync(_currentBox, input);
		} else {
			// did not replace. a new one was created previously. do nothing.
			Logger.LogDebug("{type} returned to factory. Production already complete.", typeof(TOutput).Name);
		}
	}

	async Task FillBoxAsync(TaskCompletionSource<TOutput> box, TInput input) {
		if (_disposed) {
			Logger.LogDebug("{type} will not be produced, factory is closed!", typeof(TOutput).Name);
			box.TrySetException(new ObjectDisposedException(GetType().ToString()));
			return;
		}

		try {
			Logger.LogDebug("{type} being produced...", typeof(TOutput).Name);
			var item = await _factory(input, x => OnBroken(box, x)).ConfigureAwait(false);
			_onRefresh(item);
			box.TrySetResult(item);
			Logger.LogDebug("{type} produced!", typeof(TOutput).Name);
		} catch (Exception ex) {
			await Task.Yield(); // avoid risk of stack overflow
			Logger.LogDebug(ex, "{type} production failed. Retrying in {delay}", typeof(TOutput).Name, _factoryRetryDelay);
			await Task.Delay(_factoryRetryDelay).ConfigureAwait(false);
			box.TrySetException(ex);
			OnBroken(box, _initialInput);
			//await OnBrokenAsync(box, _initialInput);
		}
	}

	public void Dispose() => _disposed = true;
}
