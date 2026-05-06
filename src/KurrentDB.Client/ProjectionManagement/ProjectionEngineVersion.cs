namespace KurrentDB.Client {
	/// <summary>
	/// The projection engine version used to execute a projection.
	/// </summary>
	/// <remarks>
	/// The engine version is pinned at projection create time and cannot be changed later.
	/// </remarks>
	public enum ProjectionEngineVersion {
		/// <summary>
		/// No engine version specified. The server treats this the same as <see cref="V1"/>.
		/// This is the default value of the enum.
		/// </summary>
		Unspecified = 0,

		/// <summary>
		/// The original projection engine. Selected by default when no engine version is specified.
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
