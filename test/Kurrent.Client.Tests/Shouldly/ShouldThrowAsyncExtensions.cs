using KurrentDB.Client;

namespace Kurrent.Client.Tests.Shouldly;

public static class ShouldThrowAsyncExtensions {
	public static Task<TException> ShouldThrowAsync<TException>(this KurrentDBClient.ReadStreamResult source) where TException : Exception =>
		source
			.ToArrayAsync()
			.AsTask()
			.ShouldThrowAsync<TException>();

	public static async Task ShouldThrowAsync<TException>(this KurrentDBClient.ReadStreamResult source, Action<TException> handler) where TException : Exception {
		var ex = await source.ShouldThrowAsync<TException>();
		handler(ex);
	}
}
