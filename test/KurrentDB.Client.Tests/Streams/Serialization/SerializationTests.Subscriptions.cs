using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using KurrentDB.Client.Core.Serialization;
using Kurrent.Diagnostics.Tracing;

namespace KurrentDB.Client.Tests.Streams.Serialization;

[Trait("Category", "Target:Streams")]
[Trait("Category", "Operation:Append")]
public class SubscriptionsSerializationTests(ITestOutputHelper output, KurrentDBPermanentFixture fixture)
	: KurrentPermanentTests<KurrentDBPermanentFixture>(output, fixture) {
	[RetryFact]
	public async Task plain_clr_objects_are_serialized_and_deserialized_using_auto_serialization() {
		// Given
		var                  stream   = Fixture.GetStreamName();
		List<UserRegistered> expected = GenerateMessages();

		//When
		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, expected);

		//Then
		var resolvedEvents = await Fixture.Streams.SubscribeToStream(stream).Take(2).ToListAsync();
		AssertThatMessages(AreDeserialized, expected, resolvedEvents);
	}

	[RetryFact]
	public async Task
		message_data_and_metadata_are_serialized_and_deserialized_using_auto_serialization_with_registered_metadata() {
		// Given
		await using var client = NewClientWith(
			serialization =>
				serialization.MessageTypeMapping.UseMetadataType<CustomMetadata>()
		);

		var stream   = Fixture.GetStreamName();
		var metadata = new CustomMetadata(Guid.NewGuid());
		var expected = GenerateMessages();
		List<Message> messagesWithMetadata =
			expected.Select(message => Message.From(message, metadata, Uuid.NewUuid())).ToList();

		// When
		await client.AppendToStreamAsync(stream, StreamState.NoStream, messagesWithMetadata);

		// Then
		var resolvedEvents = await client.SubscribeToStream(stream).Take(2).ToListAsync();
		var messages       = AssertThatMessages(AreDeserialized, expected, resolvedEvents);

		Assert.Equal(messagesWithMetadata, messages);
	}

	[RetryFact]
	public async Task
		message_metadata_is_serialized_fully_byt_deserialized_to_tracing_metadata_using_auto_serialization_WITHOUT_registered_custom_metadata() {
		var stream   = Fixture.GetStreamName();
		var metadata = new CustomMetadata(Guid.NewGuid());
		var expected = GenerateMessages();
		List<Message> messagesWithMetadata =
			expected.Select(message => Message.From(message, metadata, Uuid.NewUuid())).ToList();

		// When
		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, messagesWithMetadata);

		// Then
		var resolvedEvents = await Fixture.Streams.SubscribeToStream(stream).Take(2).ToListAsync();
		var messages       = AssertThatMessages(AreDeserialized, expected, resolvedEvents);

		Assert.Equal(messagesWithMetadata.Select(m => m with { Metadata = new TracingMetadata() }), messages);
	}

	[RetryFact]
	public async Task subscribe_to_stream_without_options_does_NOT_deserialize_resolved_message() {
		// Given
		var (stream, expected) = await AppendEventsUsingAutoSerialization();

		// When
#pragma warning disable CS0618 // Type or member is obsolete
		var resolvedEvents = await Fixture.Streams
			.SubscribeToStream(stream, FromStream.Start).Take(2)
#pragma warning restore CS0618 // Type or member is obsolete
			.ToListAsync();

		// Then
		AssertThatMessages(AreNotDeserialized, expected, resolvedEvents);
	}

	[RetryFact]
	public async Task subscribe_to_all_without_options_does_NOT_deserialize_resolved_message() {
		// Given
		var (stream, expected) = await AppendEventsUsingAutoSerialization();

		// When
#pragma warning disable CS0618 // Type or member is obsolete
		var resolvedEvents = await Fixture.Streams
			.SubscribeToAll(FromAll.Start, filterOptions: new SubscriptionFilterOptions(StreamFilter.Prefix(stream)))
#pragma warning restore CS0618 // Type or member is obsolete
			.Take(2)
			.ToListAsync();

		// Then
		AssertThatMessages(AreNotDeserialized, expected, resolvedEvents);
	}

	public static TheoryData<Action<KurrentDBClientSerializationSettings, string>> CustomTypeMappings() {
		return [
			(settings, typeName) =>
				settings.MessageTypeMapping.Register<UserRegistered>(typeName),
			(settings, typeName) =>
				settings.MessageTypeMapping.Register(typeName, typeof(UserRegistered)),
			(settings, typeName) =>
				settings.MessageTypeMapping.Register(
					new Dictionary<string, Type> { { typeName, typeof(UserRegistered) } }
				)
		];
	}

	[RetryTheory]
	[MemberData(nameof(CustomTypeMappings))]
	public async Task append_and_subscribe_to_stream_uses_custom_type_mappings(
		Action<KurrentDBClientSerializationSettings, string> customTypeMapping
	) {
		// Given
		await using var client = NewClientWith(serialization => customTypeMapping(serialization, "user_registered"));

		// When
		var (stream, expected) = await AppendEventsUsingAutoSerialization(client);

		// Then
		var resolvedEvents = await client.SubscribeToStream(stream).Take(2).ToListAsync();
		Assert.All(resolvedEvents, resolvedEvent => Assert.Equal("user_registered", resolvedEvent.Event.EventType));

		AssertThatMessages(AreDeserialized, expected, resolvedEvents);
	}

	[RetryTheory]
	[MemberData(nameof(CustomTypeMappings))]
	public async Task append_and_subscribe_to_all_uses_custom_type_mappings(
		Action<KurrentDBClientSerializationSettings, string> customTypeMapping
	) {
		// Given
		await using var client = NewClientWith(serialization => customTypeMapping(serialization, "user_registered"));

		// When
		var (stream, expected) = await AppendEventsUsingAutoSerialization(client);

		// Then
		var resolvedEvents = await client
			.SubscribeToAll(new SubscribeToAllOptions { Filter = StreamFilter.Prefix(stream) }).Take(2)
			.ToListAsync();

		Assert.All(resolvedEvents, resolvedEvent => Assert.Equal("user_registered", resolvedEvent.Event.EventType));

		AssertThatMessages(AreDeserialized, expected, resolvedEvents);
	}

	[RetryFact]
	public async Task automatic_serialization_custom_json_settings_are_applied() {
		// Given
		var systemTextJsonOptions = new JsonSerializerOptions {
			PropertyNamingPolicy = JsonNamingPolicy.KebabCaseLower,
		};

		await using var client = NewClientWith(serialization => serialization.UseJsonSettings(systemTextJsonOptions));

		// When
		var (stream, expected) = await AppendEventsUsingAutoSerialization(client);

		// Then
		var resolvedEvents = await client.SubscribeToStream(stream).Take(2).ToListAsync();
		var jsons          = resolvedEvents.Select(e => JsonDocument.Parse(e.Event.Data).RootElement).ToList();

		Assert.Equal(expected.Select(m => m.UserId), jsons.Select(j => j.GetProperty("user-id").GetGuid()));

		AssertThatMessages(AreDeserialized, expected, resolvedEvents);
	}

	public class CustomMessageTypeNamingStrategy : IMessageTypeNamingStrategy {
		public string ResolveTypeName(Type messageType, MessageTypeNamingResolutionContext resolutionContext) {
			return $"custom-{messageType}";
		}

#if NET48
		public bool TryResolveClrTypeName(EventRecord record, out string? typeName) {
#else
		public bool TryResolveClrTypeName(EventRecord record, [NotNullWhen(true)] out string? typeName) {
#endif
			var messageTypeName = record.EventType;
			typeName = messageTypeName[(messageTypeName.IndexOf('-') + 1)..];

			return true;
		}

#if NET48
		public bool TryResolveClrMetadataTypeName(EventRecord record, out string? type) {
#else
		public bool TryResolveClrMetadataTypeName(EventRecord record, [NotNullWhen(true)] out string? type) {
#endif
			type = null;
			return false;
		}
	}

	[RetryFact]
	public async Task append_and_subscribe_to_stream_uses_custom_message_type_naming_strategy() {
		// Given
		await using var client = NewClientWith(
			serialization => serialization.UseMessageTypeNamingStrategy<CustomMessageTypeNamingStrategy>()
		);

		//When
		var (stream, expected) = await AppendEventsUsingAutoSerialization(client);

		//Then
		var resolvedEvents = await Fixture.Streams.SubscribeToStream(stream).Take(2).ToListAsync();
		Assert.All(
			resolvedEvents,
			resolvedEvent => Assert.Equal($"custom-{typeof(UserRegistered).FullName}", resolvedEvent.Event.EventType)
		);

		AssertThatMessages(AreDeserialized, expected, resolvedEvents);
	}

	[RetryFact]
	public async Task append_and_subscribe_to_all_uses_custom_message_type_naming_strategy() {
		// Given
		await using var client = NewClientWith(
			serialization => serialization.UseMessageTypeNamingStrategy<CustomMessageTypeNamingStrategy>()
		);

		//When
		var (stream, expected) = await AppendEventsUsingAutoSerialization(client);

		//Then
		var resolvedEvents = await client
			.SubscribeToAll(new SubscribeToAllOptions { Filter = StreamFilter.Prefix(stream) }).Take(2)
			.ToListAsync();

		Assert.All(
			resolvedEvents,
			resolvedEvent => Assert.Equal($"custom-{typeof(UserRegistered).FullName}", resolvedEvent.Event.EventType)
		);

		AssertThatMessages(AreDeserialized, expected, resolvedEvents);
	}

	[RetryFact]
	public async Task
		subscribe_to_stream_deserializes_resolved_message_appended_with_manual_compatible_serialization() {
		// Given
		var (stream, expected) = await AppendEventsUsingManualSerialization(
			message => $"stream-{message.GetType().FullName!}"
		);

		// When
		var resolvedEvents = await Fixture.Streams.SubscribeToStream(stream).Take(2).ToListAsync();

		// Then
		AssertThatMessages(AreDeserialized, expected, resolvedEvents);
	}

	[RetryFact]
	public async Task subscribe_to_all_deserializes_resolved_message_appended_with_manual_compatible_serialization() {
		// Given
		var (stream, expected) = await AppendEventsUsingManualSerialization(
			message => $"stream-{message.GetType().FullName!}"
		);

		// When
		var resolvedEvents = await Fixture.Streams
			.SubscribeToAll(new SubscribeToAllOptions { Filter = StreamFilter.Prefix(stream) }).Take(2)
			.ToListAsync();

		// Then
		AssertThatMessages(AreDeserialized, expected, resolvedEvents);
	}

	[RetryFact]
	public async Task
		subscribe_to_stream_does_NOT_deserialize_resolved_message_appended_with_manual_incompatible_serialization() {
		// Given
		var (stream, expected) = await AppendEventsUsingManualSerialization(_ => "user_registered");

		// When
		var resolvedEvents = await Fixture.Streams.SubscribeToStream(stream).Take(2).ToListAsync();

		// Then
		AssertThatMessages(AreNotDeserialized, expected, resolvedEvents);
	}

	[RetryFact]
	public async Task
		subscribe_to_all_does_NOT_deserialize_resolved_message_appended_with_manual_incompatible_serialization() {
		// Given
		var (stream, expected) = await AppendEventsUsingManualSerialization(_ => "user_registered");

		// When
		var resolvedEvents = await Fixture.Streams
			.SubscribeToAll(new SubscribeToAllOptions { Filter = StreamFilter.Prefix(stream) }).Take(2)
			.ToListAsync();

		// Then
		AssertThatMessages(AreNotDeserialized, expected, resolvedEvents);
	}

	static List<Message> AssertThatMessages(
		Action<UserRegistered, ResolvedEvent> assertMatches,
		List<UserRegistered> expected,
		List<ResolvedEvent> resolvedEvents
	) {
		Assert.Equal(expected.Count, resolvedEvents.Count);
		Assert.NotEmpty(resolvedEvents);

		Assert.All(resolvedEvents, (resolvedEvent, idx) => assertMatches(expected[idx], resolvedEvent));

		return resolvedEvents.Select(resolvedEvent => resolvedEvent.Message!).ToList();
	}

	static void AreDeserialized(UserRegistered expected, ResolvedEvent resolvedEvent) {
		Assert.NotNull(resolvedEvent.Message);
		Assert.Equal(expected, resolvedEvent.Message.Data);
		Assert.Equal(expected, resolvedEvent.DeserializedData);
	}

	static void AreNotDeserialized(UserRegistered expected, ResolvedEvent resolvedEvent) {
		Assert.Null(resolvedEvent.Message);
		Assert.Equal(
			expected,
			JsonSerializer.Deserialize<UserRegistered>(
				resolvedEvent.Event.Data.Span,
				SystemTextJsonSerializationSettings.DefaultJsonSerializerOptions
			)
		);
	}

	async Task<(string, List<UserRegistered>)> AppendEventsUsingAutoSerialization(
		KurrentDBClient? kurrentDbClient = null
	) {
		var stream   = Fixture.GetStreamName();
		var messages = GenerateMessages();

		var writeResult =
			await (kurrentDbClient ?? Fixture.Streams).AppendToStreamAsync(stream, StreamState.Any, messages);

		Assert.Equal((ulong)messages.Count - 1, writeResult.NextExpectedStreamState);

		return (stream, messages);
	}

	async Task<(string, List<UserRegistered>)> AppendEventsUsingManualSerialization(
		Func<UserRegistered, string> getTypeName
	) {
		var stream   = Fixture.GetStreamName();
		var messages = GenerateMessages();
		var eventData = messages.Select(
			message =>
				new MessageData(
					getTypeName(message),
					Encoding.UTF8.GetBytes(
						JsonSerializer.Serialize(
							message,
							SystemTextJsonSerializationSettings.DefaultJsonSerializerOptions
						)
					)
				)
		);

		var writeResult = await Fixture.Streams.AppendToStreamAsync(stream, StreamState.Any, eventData);
		Assert.Equal((ulong)messages.Count - 1, writeResult.NextExpectedStreamState);

		return (stream, messages);
	}

	static List<UserRegistered> GenerateMessages(int count = 2) =>
		Enumerable.Range(0, count)
			.Select(
				_ => new UserRegistered(
					Guid.NewGuid(),
					new Address(Guid.NewGuid().ToString(), Guid.NewGuid().GetHashCode())
				)
			).ToList();

	KurrentDBClient NewClientWith(Action<KurrentDBClientSerializationSettings> customizeSerialization) {
		var settings = Fixture.DbClientSettings;
		settings.Serialization = settings.Serialization.Clone();
		customizeSerialization(settings.Serialization);

		return new KurrentDBClient(settings);
	}

	public record Address(string Street, int Number);

	public record UserRegistered(Guid UserId, Address Address);

	public record CustomMetadata(Guid UserId);
}
