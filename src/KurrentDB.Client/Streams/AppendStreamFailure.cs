using KurrentDB.Protocol.V2;

namespace KurrentDB.Client;

public struct AppendStreamFailure {
	public string  Stream                      { get; }
	public string? AccessDenied                { get; }
	public long?   WrongExpectedStreamRevision { get; }
	public bool    IsStreamDeleted             { get; } = false;
	public int?    TransactionMaxSizeExceeded  { get; }

	internal AppendStreamFailure(KurrentDB.Protocol.V2.AppendStreamFailure grpcFailure) {
		Stream = grpcFailure.Stream;

		switch (grpcFailure.ErrorCase) {
			case Protocol.V2.AppendStreamFailure.ErrorOneofCase.AccessDenied:
				AccessDenied = grpcFailure.AccessDenied.Reason;
				break;

			case Protocol.V2.AppendStreamFailure.ErrorOneofCase.StreamDeleted: IsStreamDeleted = true;
				break;

			case Protocol.V2.AppendStreamFailure.ErrorOneofCase.WrongExpectedRevision:
				WrongExpectedStreamRevision = grpcFailure.WrongExpectedRevision.StreamRevision;
				break;

			case Protocol.V2.AppendStreamFailure.ErrorOneofCase.TransactionMaxSizeExceeded:
				TransactionMaxSizeExceeded = grpcFailure.TransactionMaxSizeExceeded.MaxSize;
				break;

			default:
				throw new ArgumentOutOfRangeException(nameof(grpcFailure));
		}
	}
}
