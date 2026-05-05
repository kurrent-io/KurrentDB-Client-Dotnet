namespace KurrentDB.Client {
	/// <summary>
	/// The projection engine version used to execute a projection.
	/// </summary>
	/// <remarks>
	/// The engine version is pinned at projection create time and cannot be changed later.
	/// </remarks>
	public enum ProjectionEngineVersion {
		/// <summary>
		/// The original projection engine. This is the default.
		/// </summary>
		V1 = 1,

		/// <summary>
		/// The next-generation projection engine that processes partitions in parallel.
		/// V2 is opt-in and does not support <c>trackEmittedStreams</c>, bi-state projections,
		/// or live <c>outputState</c> result streams. See the KurrentDB documentation for the full list of
		/// limitations before choosing V2.
		/// </summary>
		V2 = 2
	}
}
