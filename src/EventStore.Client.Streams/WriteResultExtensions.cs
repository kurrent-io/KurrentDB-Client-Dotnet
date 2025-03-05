namespace EventStore.Client {
	internal static class WriteResultExtensions {
		public static IWriteResult OptionallyThrowWrongExpectedVersionException(this IWriteResult writeResult,
			EventStoreClientOperationOptions options) =>
			(options.ThrowOnAppendFailure, writeResult) switch {
				(true, WrongExpectedVersionResult wrongExpectedVersionResult)
					=> throw new WrongExpectedStreamStateException(wrongExpectedVersionResult.StreamName,
						writeResult.NextExpectedStreamState, wrongExpectedVersionResult.ActualStreamState),
				_ => writeResult
			};
	}
}
