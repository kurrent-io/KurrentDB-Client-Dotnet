namespace KurrentDb.Client {
	internal static class WriteResultExtensions {
		public static IWriteResult OptionallyThrowWrongExpectedVersionException(this IWriteResult writeResult,
			KurrentDbClientOperationOptions options) =>
			(options.ThrowOnAppendFailure, writeResult) switch {
				(true, WrongExpectedVersionResult wrongExpectedVersionResult)
					=> throw new WrongExpectedVersionException(wrongExpectedVersionResult.StreamName,
						writeResult.NextExpectedStreamRevision, wrongExpectedVersionResult.ActualStreamRevision),
				_ => writeResult
			};
	}
}
