using System.Reflection;
using AutoFixture;

namespace KurrentDB.Client.Tests;

[Trait("Category", "Target:Misc")]
public class StreamStateTests : ValueObjectTests<StreamState> {
	public StreamStateTests() : base(new ScenarioFixture()) { }

	public static IEnumerable<object?[]> ArgumentOutOfRangeTestCases() {
		yield return [-3];
	}

	[Theory]
	[MemberData(nameof(ArgumentOutOfRangeTestCases))]
	public void ArgumentOutOfRange(int value) {
		var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new StreamState(value));
		Assert.Equal(nameof(value), ex.ParamName);
	}

	[RetryFact]
	public void ImplicitConversionExpectedResult() {
		const ulong expected = 1;
		Assert.Equal(expected, StreamState.StreamRevision(expected));
	}

	public static IEnumerable<object?[]> ToStringTestCases() {
		yield return [StreamState.Any, nameof(StreamState.Any)];
		yield return [StreamState.NoStream, nameof(StreamState.NoStream)];
		yield return [StreamState.StreamExists, nameof(StreamState.StreamExists)];
	}

	[Theory]
	[MemberData(nameof(ToStringTestCases))]
	public void ToStringExpectedResult(StreamState sut, string expected) => Assert.Equal(expected, sut.ToString());

	class ScenarioFixture : Fixture {
		static readonly StreamState[] Instances = Array.ConvertAll(
			typeof(StreamState)
				.GetFields(BindingFlags.Public | BindingFlags.Static),
			fi => (StreamState)fi.GetValue(null)!
		);

		static int RefCount;

		public ScenarioFixture() =>
			Customize<StreamState>(composer => composer.FromFactory(() => Instances[Interlocked.Increment(ref RefCount) % Instances.Length]));
	}
}
