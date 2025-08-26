using KurrentDB.Client;

namespace Kurrent.Client.Tests.Legacy;

[Category("Legacy")]
public class UuidTests {
	[Test]
	public void to_guid_returns_expected_result() {
		var guid = Guid.NewGuid();
		var sut  = Uuid.FromGuid(guid);

		sut.ToGuid().ShouldBe(guid);
	}

	[Test]
	public void to_string_produces_expected_result() {
		var sut = Uuid.NewUuid();

		sut.ToString().ShouldBe(sut.ToGuid().ToString());
	}

	[Test]
	public void to_formatted_string_produces_expected_result() {
		var sut = Uuid.NewUuid();

		sut.ToString("n").ShouldBe(sut.ToGuid().ToString("n"));
	}

	[Test]
	public void to_dto_returns_expected_result() {
		var msb = GetRandomInt64();
		var lsb = GetRandomInt64();

		var sut = Uuid.FromInt64(msb, lsb);

		var result = sut.ToDto();

		result.Structured.ShouldNotBeNull();
		result.Structured.LeastSignificantBits.ShouldBe(lsb);
		result.Structured.MostSignificantBits.ShouldBe(msb);
	}

	[Test]
	public void parse_returns_expected_result() {
		var guid = Guid.NewGuid();

		var sut = Uuid.Parse(guid.ToString());

		sut.ShouldBe(Uuid.FromGuid(guid));
	}

	[Test]
	public void from_int64_returns_expected_result() {
		var guid     = Guid.Parse("65678f9b-d139-4786-8305-b9166922b378");
		var sut      = Uuid.FromInt64(7306966819824813958L, -9005588373953137800L);
		var expected = Uuid.FromGuid(guid);

		sut.ShouldBe(expected);
	}

	static long GetRandomInt64() {
		var buffer = new byte[sizeof(long)];

		new Random().NextBytes(buffer);

		return BitConverter.ToInt64(buffer, 0);
	}
}
