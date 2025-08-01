using KurrentDB.Client;
using KurrentDB.Client.Tests.TestNode;

namespace KurrentDB.Client.Tests;

[Trait("Category", "Target:Security")]
public class OverridenUserStreamSecurityTests(ITestOutputHelper output, OverridenUserStreamSecurityTests.CustomFixture fixture)
	: KurrentTemporaryTests<OverridenUserStreamSecurityTests.CustomFixture>(output, fixture) {
	[Fact]
	public async Task operations_on_user_stream_succeeds_for_authorized_user() {
		var stream = Fixture.GetStreamName();
		await Fixture.AppendStream(stream, TestCredentials.TestUser1);

		await Fixture.ReadEvent(stream, TestCredentials.TestUser1);
		await Fixture.ReadStreamForward(stream, TestCredentials.TestUser1);
		await Fixture.ReadStreamBackward(stream, TestCredentials.TestUser1);

		await Fixture.ReadMeta(stream, TestCredentials.TestUser1);
		await Fixture.WriteMeta(stream, TestCredentials.TestUser1);

		await Fixture.SubscribeToStream(stream, TestCredentials.TestUser1);

		await Fixture.DeleteStream(stream, TestCredentials.TestUser1);
	}

	[Fact]
	public async Task operations_on_user_stream_fail_for_not_authorized_user() {
		var stream = Fixture.GetStreamName();
		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.ReadEvent(stream, TestCredentials.TestUser2));
		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.ReadStreamForward(stream, TestCredentials.TestUser2));
		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.ReadStreamBackward(stream, TestCredentials.TestUser2));

		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.AppendStream(stream, TestCredentials.TestUser2));
		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.ReadMeta(stream, TestCredentials.TestUser2));
		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.WriteMeta(stream, TestCredentials.TestUser2));

		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.SubscribeToStream(stream, TestCredentials.TestUser2));

		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.DeleteStream(stream, TestCredentials.TestUser2));
	}

	[Fact]
	public async Task operations_on_user_stream_fail_for_anonymous_user() {
		var stream = Fixture.GetStreamName();
		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.ReadEvent(stream));
		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.ReadStreamForward(stream));
		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.ReadStreamBackward(stream));

		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.AppendStream(stream));
		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.ReadMeta(stream));
		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.WriteMeta(stream));

		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.SubscribeToStream(stream));

		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.DeleteStream(stream));
	}

	[Fact]
	public async Task operations_on_user_stream_succeed_for_admin() {
		var stream = Fixture.GetStreamName();
		await Fixture.AppendStream(stream, TestCredentials.TestAdmin);

		await Fixture.ReadEvent(stream, TestCredentials.TestAdmin);
		await Fixture.ReadStreamForward(stream, TestCredentials.TestAdmin);
		await Fixture.ReadStreamBackward(stream, TestCredentials.TestAdmin);

		await Fixture.ReadMeta(stream, TestCredentials.TestAdmin);
		await Fixture.WriteMeta(stream, TestCredentials.TestAdmin);

		await Fixture.SubscribeToStream(stream, TestCredentials.TestAdmin);

		await Fixture.DeleteStream(stream, TestCredentials.TestAdmin);
	}

	public class CustomFixture : SecurityFixture {
		protected override Task When() {
			var settings = new SystemSettings(new("user1", "user1", "user1", "user1", "user1"));
			return Streams.SetSystemSettingsAsync(settings, userCredentials: TestCredentials.TestAdmin);
		}
	}
}
