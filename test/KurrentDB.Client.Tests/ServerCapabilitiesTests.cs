namespace KurrentDB.Client.Tests;

[Trait("Category", "Target:Misc")]
public class ServerCapabilitiesTests {
	[Theory]
	[InlineData("26.2.2", true)]
	[InlineData("26.2.10", true)]
	[InlineData("26.3.0", true)]
	[InlineData("27.0.0", true)]
	[InlineData("26.2.2.5", true)]
	[InlineData("26.2.1", false)]
	[InlineData("26.2.0", false)]
	[InlineData("25.1.9", false)]
	[InlineData("24.10.16", false)]
	[InlineData("0.0.0", false)]
	[InlineData("26.2", false)]
	[InlineData("26.2.x", false)]
	[InlineData("26.-1.0", false)]
	[InlineData("-1.2.3", false)]
	[InlineData("26.2.2-rc.1", true)]
	[InlineData("26.3.0-prerelease", true)]
	[InlineData("26.2.0-prerelease", false)]
	[InlineData("26.2.-1", false)]
	[InlineData("26.2.", false)]
	[InlineData(" 26.2.2", false)]
	[InlineData("v26.2.2", false)]
	[InlineData("99999999999.1.1", false)]
	[InlineData("", false)]
	[InlineData(null, false)]
	public void subscription_fell_behind_is_supported_from_26_2_2(string? serverVersion, bool expected) =>
		Assert.Equal(expected, ServerCapabilities.VersionSupportsSubscriptionFellBehind(serverVersion));

	[Fact]
	public void subscription_fell_behind_is_not_supported_by_default() =>
		Assert.False(new ServerCapabilities().SupportsSubscriptionFellBehind);
}
