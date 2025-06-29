namespace KurrentDB.Client {
	internal static class WriteResultExtensions {
		public static IWriteResult OptionallyThrowWrongExpectedVersionException(
			this IWriteResult writeResult,
			KurrentDBClientOperationOptions options
		) =>
			(options.ThrowOnAppendFailure, writeResult) switch {
				(true, WrongExpectedVersionResult wrongExpectedVersionResult)
					=> throw new WrongExpectedVersionException(
						wrongExpectedVersionResult.StreamName,
						writeResult.NextExpectedStreamState,
						wrongExpectedVersionResult.ActualStreamState
					),
				_ => writeResult
			};
	}
}
