namespace KurrentDB.Client {
	// compatibility levels the client can declare when reading and subscribing. each includes the ones below it.
	static class ReadCompatibilityLevel {
		// stream reads receive the first and last stream position
		public const uint Level1_StreamPositions = 1;

		// subscriptions receive FellBehind
		public const uint Level2_FellBehindMessage = 2;
	}
}
