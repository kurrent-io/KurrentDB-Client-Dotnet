namespace KurrentDB.Client {
	/// <summary>
	/// The features of a subscription.
	/// </summary>
	public sealed record StreamSubscriptionFeatures {
		/// <summary>
		/// Whether <see cref="KurrentDBClientSettings.EnableRichSubscriptionLiveness"/> is enabled and the node
		/// supports it, as the node reported when the client connected.
		/// </summary>
		/// <remarks>
		/// When false, never receiving <see cref="StreamMessage.FellBehind"/> does not mean the subscription stayed live.
		/// </remarks>
		public bool RichLiveness { get; init; }
	}
}
