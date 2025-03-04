// ReSharper disable CheckNamespace

using System.Diagnostics;
using KurrentDb.Client;

namespace Shouldly;

[DebuggerStepThrough]
public static class ShouldThrowAsyncExtensions {
	public static Task<TException> ShouldThrowAsync<TException>(this KurrentDBClient.ReadStreamResult source) where TException : Exception =>
		source.ToArrayAsync().AsTask().ShouldThrowAsync<TException>();
}
