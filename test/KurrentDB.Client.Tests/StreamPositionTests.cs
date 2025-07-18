using AutoFixture;

namespace KurrentDB.Client.Tests;

[Trait("Category", "Target:Misc")]
public class StreamPositionTests : ValueObjectTests<StreamPosition> {
	public StreamPositionTests() : base(new ScenarioFixture()) { }

	[RetryFact]
	public void IsComparable() => Assert.IsAssignableFrom<IComparable<StreamPosition>>(_fixture.Create<StreamPosition>());

	[RetryFact]
	public void AdditionOperator() {
		var sut = StreamPosition.Start;
		Assert.Equal(new(1), sut + 1);
		Assert.Equal(new(1), 1 + sut);
	}

	[RetryFact]
	public void NextReturnsExpectedResult() {
		var sut = StreamPosition.Start;
		Assert.Equal(sut + 1, sut.Next());
	}

	public static IEnumerable<object?[]> AdditionOutOfBoundsCases() {
		yield return new object?[] { StreamPosition.End, 1 };
		yield return new object?[] { new StreamPosition(long.MaxValue), long.MaxValue + 2UL };
	}

	[Theory]
	[MemberData(nameof(AdditionOutOfBoundsCases))]
	public void AdditionOutOfBoundsThrows(StreamPosition StreamPosition, ulong operand) {
		Assert.Throws<OverflowException>(() => StreamPosition + operand);
		Assert.Throws<OverflowException>(() => operand + StreamPosition);
	}

	[RetryFact]
	public void SubtractionOperator() {
		var sut = new StreamPosition(1);
		Assert.Equal(new(0), sut - 1);
		Assert.Equal(new(0), 1 - sut);
	}

	public static IEnumerable<object?[]> SubtractionOutOfBoundsCases() {
		yield return new object?[] { new StreamPosition(1), 2 };
		yield return new object?[] { StreamPosition.Start, 1 };
	}

	[Theory]
	[MemberData(nameof(SubtractionOutOfBoundsCases))]
	public void SubtractionOutOfBoundsThrows(StreamPosition streamPosition, ulong operand) {
		Assert.Throws<OverflowException>(() => streamPosition - operand);
		Assert.Throws<OverflowException>(() => (ulong)streamPosition - new StreamPosition(operand));
	}

	public static IEnumerable<object?[]> ArgumentOutOfRangeTestCases() {
		yield return new object?[] { long.MaxValue + 1UL };
		yield return new object?[] { ulong.MaxValue - 1UL };
	}

	[Theory]
	[MemberData(nameof(ArgumentOutOfRangeTestCases))]
	public void ArgumentOutOfRange(ulong value) {
		var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new StreamPosition(value));
		Assert.Equal(nameof(value), ex.ParamName);
	}

	[RetryFact]
	public void FromStreamPositionReturnsExpectedResult() {
		var result = StreamPosition.FromStreamRevision(0);

		Assert.Equal(new(0), result);
	}

	[RetryFact]
	public void ExplicitConversionToUInt64ReturnsExpectedResult() {
		const ulong value = 0UL;

		var actual = (ulong)new StreamPosition(value);
		Assert.Equal(value, actual);
	}

	[RetryFact]
	public void ImplicitConversionToUInt64ReturnsExpectedResult() {
		const ulong value = 0UL;

		ulong actual = new StreamPosition(value);
		Assert.Equal(value, actual);
	}

	[RetryFact]
	public void ExplicitConversionToStreamPositionReturnsExpectedResult() {
		const ulong value = 0UL;

		var expected = new StreamPosition(value);
		var actual   = (StreamPosition)value;
		Assert.Equal(expected, actual);
	}

	[RetryFact]
	public void ImplicitConversionToStreamPositionReturnsExpectedResult() {
		const ulong value = 0UL;

		var expected = new StreamPosition(value);

		StreamPosition actual = value;
		Assert.Equal(expected, actual);
	}

	[RetryFact]
	public void ToStringExpectedResult() {
		var expected = 0UL.ToString();

		Assert.Equal(expected, new StreamPosition(0UL).ToString());
	}

	[RetryFact]
	public void ToUInt64ExpectedResult() {
		var expected = 0UL;

		Assert.Equal(expected, new StreamPosition(expected).ToUInt64());
	}

	class ScenarioFixture : Fixture {
		public ScenarioFixture() => Customize<StreamPosition>(composer => composer.FromFactory<ulong>(value => new(value)));
	}
}
