using KurrentDb.Client;

namespace KurrentDb.Client.Tests;

public static class ShouldThrowAsyncExtensions {
	public static Task<TException> ShouldThrowAsync<TException>(this KurrentDbDbClient.ReadStreamResult source) where TException : Exception =>
		source
			.ToArrayAsync()
			.AsTask()
			.ShouldThrowAsync<TException>();

	public static async Task ShouldThrowAsync<TException>(this KurrentDbDbClient.ReadStreamResult source, Action<TException> handler) where TException : Exception {
		var ex = await source.ShouldThrowAsync<TException>();
		handler(ex);
	}
}
