namespace KurrentDB.Client {
	sealed class ReadLifetime : IDisposable {
		readonly CancellationTokenSource _cts = new();
		CancellationTokenRegistration    _callerRegistration;
		int                              _released;

		public ReadLifetime(CancellationToken caller) {
			Token               = _cts.Token;
			_callerRegistration = caller.Register(static s => ((CancellationTokenSource)s!).Cancel(), _cts);
		}

		public CancellationToken Token { get; }

		public void Detach() => _callerRegistration.Dispose();

		public void Dispose() {
			if (Interlocked.Exchange(ref _released, 1) == 1)
				return;

			_callerRegistration.Dispose();
			try {
				_cts.Cancel();
			}
			finally {
				_cts.Dispose();
			}
		}
	}
}
