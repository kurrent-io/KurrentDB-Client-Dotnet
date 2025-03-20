using System.Runtime.CompilerServices;
using KurrentDB.Client;
using KurrentDB.Client.Tests.TestNode;

public class SecurityFixture : KurrentDBTemporaryFixture {
	public const string NoAclStream       = nameof(NoAclStream);
	public const string ReadStream        = nameof(ReadStream);
	public const string WriteStream       = nameof(WriteStream);
	public const string MetaReadStream    = nameof(MetaReadStream);
	public const string MetaWriteStream   = nameof(MetaWriteStream);
	public const string AllStream         = SystemStreams.AllStream;
	public const string NormalAllStream   = nameof(NormalAllStream);
	public const string SystemAllStream   = $"${nameof(SystemAllStream)}";
	public const string SystemAdminStream = $"${nameof(SystemAdminStream)}";
	public const string SystemAclStream   = $"${nameof(SystemAclStream)}";

	const int TimeoutMs = 1000;

	public SecurityFixture() : base(x => x.WithoutDefaultCredentials()) {
		OnSetup = async () => {
			await DbUsers.CreateUserWithRetry(
				TestCredentials.TestUser1.Username!,
				nameof(TestCredentials.TestUser1),
				Array.Empty<string>(),
				TestCredentials.TestUser1.Password!,
				TestCredentials.Root
			).WithTimeout(TimeSpan.FromMilliseconds(TimeoutMs));

			await DbUsers.CreateUserWithRetry(
				TestCredentials.TestUser2.Username!,
				nameof(TestCredentials.TestUser2),
				Array.Empty<string>(),
				TestCredentials.TestUser2.Password!,
				TestCredentials.Root
			).WithTimeout(TimeSpan.FromMilliseconds(TimeoutMs));

			await DbUsers.CreateUserWithRetry(
				TestCredentials.TestAdmin.Username!,
				nameof(TestCredentials.TestAdmin),
				new[] { SystemRoles.Admins },
				TestCredentials.TestAdmin.Password!,
				TestCredentials.Root
			).WithTimeout(TimeSpan.FromMilliseconds(TimeoutMs));

			await Given();
			await When();
		};
	}

	protected virtual async Task Given() {
		await Streams.SetStreamMetadataAsync(
			NoAclStream,
			StreamState.NoStream,
			new(),
			new SetStreamMetadataOptions { UserCredentials = TestCredentials.TestAdmin }
		).WithTimeout(TimeSpan.FromMilliseconds(TimeoutMs));

		await Streams.SetStreamMetadataAsync(
			ReadStream,
			StreamState.NoStream,
			new(acl: new(TestCredentials.TestUser1.Username)),
			new SetStreamMetadataOptions { UserCredentials = TestCredentials.TestAdmin }
		).WithTimeout(TimeSpan.FromMilliseconds(TimeoutMs));

		await Streams.SetStreamMetadataAsync(
			WriteStream,
			StreamState.NoStream,
			new(acl: new(writeRole: TestCredentials.TestUser1.Username)),
			new SetStreamMetadataOptions { UserCredentials = TestCredentials.TestAdmin }
		).WithTimeout(TimeSpan.FromMilliseconds(TimeoutMs));

		await Streams.SetStreamMetadataAsync(
			MetaReadStream,
			StreamState.NoStream,
			new(acl: new(metaReadRole: TestCredentials.TestUser1.Username)),
			new SetStreamMetadataOptions { UserCredentials = TestCredentials.TestAdmin }
		).WithTimeout(TimeSpan.FromMilliseconds(TimeoutMs));

		await Streams.SetStreamMetadataAsync(
			MetaWriteStream,
			StreamState.NoStream,
			new(acl: new(metaWriteRole: TestCredentials.TestUser1.Username)),
			new SetStreamMetadataOptions { UserCredentials = TestCredentials.TestAdmin }
		).WithTimeout(TimeSpan.FromMilliseconds(TimeoutMs));

		await Streams.SetStreamMetadataAsync(
			AllStream,
			StreamState.Any,
			new(acl: new(TestCredentials.TestUser1.Username)),
			new SetStreamMetadataOptions { UserCredentials = TestCredentials.TestAdmin }
		).WithTimeout(TimeSpan.FromMilliseconds(TimeoutMs));

		await Streams.SetStreamMetadataAsync(
			SystemAclStream,
			StreamState.NoStream,
			new(
				acl: new(
					writeRole: TestCredentials.TestUser1.Username,
					readRole: TestCredentials.TestUser1.Username,
					metaWriteRole: TestCredentials.TestUser1.Username,
					metaReadRole: TestCredentials.TestUser1.Username
				)
			),
			new SetStreamMetadataOptions { UserCredentials = TestCredentials.TestAdmin }
		).WithTimeout(TimeSpan.FromMilliseconds(TimeoutMs));

		await Streams.SetStreamMetadataAsync(
			SystemAdminStream,
			StreamState.NoStream,
			new(
				acl: new(
					writeRole: SystemRoles.Admins,
					readRole: SystemRoles.Admins,
					metaWriteRole: SystemRoles.Admins,
					metaReadRole: SystemRoles.Admins
				)
			),
			new SetStreamMetadataOptions { UserCredentials = TestCredentials.TestAdmin }
		).WithTimeout(TimeSpan.FromMilliseconds(TimeoutMs));

		await Streams.SetStreamMetadataAsync(
			NormalAllStream,
			StreamState.NoStream,
			new(
				acl: new(
					writeRole: SystemRoles.All,
					readRole: SystemRoles.All,
					metaWriteRole: SystemRoles.All,
					metaReadRole: SystemRoles.All
				)
			),
			new SetStreamMetadataOptions { UserCredentials = TestCredentials.TestAdmin }
		).WithTimeout(TimeSpan.FromMilliseconds(TimeoutMs));

		await Streams.SetStreamMetadataAsync(
			SystemAllStream,
			StreamState.NoStream,
			new(
				acl: new(
					writeRole: SystemRoles.All,
					readRole: SystemRoles.All,
					metaWriteRole: SystemRoles.All,
					metaReadRole: SystemRoles.All
				)
			),
			new SetStreamMetadataOptions { UserCredentials = TestCredentials.TestAdmin }
		).WithTimeout(TimeSpan.FromMilliseconds(TimeoutMs));
	}

	protected virtual Task When() => Task.CompletedTask;

	public Task ReadEvent(string streamId, UserCredentials? userCredentials = null) =>
		Streams.ReadStreamAsync(streamId, new ReadStreamOptions { MaxCount = 1, UserCredentials = userCredentials })
			.ToArrayAsync()
			.AsTask()
			.WithTimeout(TimeSpan.FromMilliseconds(TimeoutMs));

	public Task ReadStreamForward(string streamId, UserCredentials? userCredentials = null) =>
		Streams.ReadStreamAsync(streamId, new ReadStreamOptions { MaxCount = 1, UserCredentials = userCredentials })
			.ToArrayAsync()
			.AsTask()
			.WithTimeout(TimeSpan.FromMilliseconds(TimeoutMs));

	public Task ReadStreamBackward(string streamId, UserCredentials? userCredentials = null) =>
		Streams.ReadStreamAsync(
				streamId,
				new ReadStreamOptions {
					Direction       = Direction.Backwards,
					StreamPosition  = StreamPosition.End,
					MaxCount        = 1,
					UserCredentials = userCredentials
				}
			)
			.ToArrayAsync()
			.AsTask()
			.WithTimeout(TimeSpan.FromMilliseconds(TimeoutMs));

	public Task<IWriteResult> AppendStream(string streamId, UserCredentials? userCredentials = null) =>
		Streams.AppendToStreamAsync(
				streamId,
				StreamState.Any,
				CreateTestEvents(3),
				new AppendToStreamOptions { UserCredentials = userCredentials }
			)
			.WithTimeout(TimeSpan.FromMilliseconds(TimeoutMs));

	public Task ReadAllForward(UserCredentials? userCredentials = null) =>
		Streams.ReadAllAsync(new ReadAllOptions { MaxCount = 1, UserCredentials = userCredentials })
			.ToArrayAsync()
			.AsTask()
			.WithTimeout(TimeSpan.FromMilliseconds(TimeoutMs));

	public Task ReadAllBackward(UserCredentials? userCredentials = null) =>
		Streams.ReadAllAsync(
				new ReadAllOptions {
					Direction       = Direction.Backwards, 
					Position = Position.End, 
					MaxCount = 1,
					UserCredentials = userCredentials
				}
			)
			.ToArrayAsync()
			.AsTask()
			.WithTimeout(TimeSpan.FromMilliseconds(TimeoutMs));

	public Task<StreamMetadataResult> ReadMeta(string streamId, UserCredentials? userCredentials = null) =>
		Streams.GetStreamMetadataAsync(streamId, new OperationOptions { UserCredentials = userCredentials })
			.WithTimeout(TimeSpan.FromMilliseconds(TimeoutMs));

	public Task<IWriteResult> WriteMeta(
		string streamId, UserCredentials? userCredentials = null, string? role = null
	) =>
		Streams.SetStreamMetadataAsync(
				streamId,
				StreamState.Any,
				new(
					acl: new(
						writeRole: role,
						readRole: role,
						metaWriteRole: role,
						metaReadRole: role
					)
				),
				new SetStreamMetadataOptions { UserCredentials = userCredentials }
			)
			.WithTimeout(TimeSpan.FromMilliseconds(TimeoutMs));

	public async Task SubscribeToStream(string streamId, UserCredentials? userCredentials = null) {
		await using var subscription =
			Streams.SubscribeToStream(streamId, FromStream.Start, userCredentials: userCredentials);

		await subscription
			.Messages.OfType<StreamMessage.SubscriptionConfirmation>().AnyAsync().AsTask()
			.WithTimeout(TimeSpan.FromMilliseconds(TimeoutMs));
	}

	public async Task SubscribeToAll(UserCredentials? userCredentials = null) {
		await using var subscription =
			Streams.SubscribeToAll(FromAll.Start, userCredentials: userCredentials);

		await subscription
			.Messages.OfType<StreamMessage.SubscriptionConfirmation>().AnyAsync().AsTask()
			.WithTimeout(TimeSpan.FromMilliseconds(TimeoutMs));
	}

	public async Task<string> CreateStreamWithMeta(
		StreamMetadata metadataPermanent, [CallerMemberName] string streamId = "<unknown>"
	) {
		await Streams.SetStreamMetadataAsync(
				streamId,
				StreamState.NoStream,
				metadataPermanent,
				new SetStreamMetadataOptions { UserCredentials = TestCredentials.TestAdmin }
			)
			.WithTimeout(TimeSpan.FromMilliseconds(TimeoutMs));

		return streamId;
	}

	public Task<DeleteResult> DeleteStream(string streamId, UserCredentials? userCredentials = null) =>
		Streams.TombstoneAsync(streamId, StreamState.Any, userCredentials: userCredentials)
			.WithTimeout(TimeSpan.FromMilliseconds(TimeoutMs));
}
