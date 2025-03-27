using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using KurrentDB.Client.Core.Serialization;

namespace KurrentDB.Client.Tests.Core.Serialization;

public class SchemaRegistryTests {
	// Test classes
	record TestEvent1;

	record TestEvent2;

	record TestEvent3;

	record TestMetadata;

	[Fact]
	public void GetSerializer_ReturnsCorrectSerializer() {
		// Given
		var jsonSerializer  = new SystemTextJsonSerializer();
		var bytesSerializer = new SystemTextJsonSerializer();

		var serializers = new Dictionary<ContentType, ISerializer> {
			{ ContentType.Json, jsonSerializer },
			{ ContentType.Bytes, bytesSerializer }
		};

		var registry = new SchemaRegistry(
			serializers,
			new DefaultMessageTypeNamingStrategy(typeof(TestMetadata))
		);

		// When
		var resultJsonSerializer  = registry.GetSerializer(ContentType.Json);
		var resultBytesSerializer = registry.GetSerializer(ContentType.Bytes);

		// Then
		Assert.NotSame(resultJsonSerializer, resultBytesSerializer);
		Assert.Same(jsonSerializer, resultJsonSerializer);
		Assert.Same(bytesSerializer, resultBytesSerializer);
	}

	[Fact]
	public void From_WithDefaultSettings_CreatesRegistryWithDefaults() {
		// Given
		var settings = new KurrentDBClientSerializationSettings();

		// When
		var registry = SchemaRegistry.From(settings);

		// Then
		Assert.NotNull(registry);
		Assert.NotNull(registry.GetSerializer(ContentType.Json));
		Assert.NotNull(registry.GetSerializer(ContentType.Bytes));

		Assert.IsType<SystemTextJsonSerializer>(registry.GetSerializer(ContentType.Json));
		Assert.IsType<SystemTextJsonSerializer>(registry.GetSerializer(ContentType.Bytes));
	}

	[Fact]
	public void From_WithCustomJsonSerializer_UsesProvidedSerializer() {
		// Given
		var customJsonSerializer = new SystemTextJsonSerializer(
			new SystemTextJsonSerializationSettings {
				Options = new JsonSerializerOptions { WriteIndented = true }
			}
		);

		var settings = new KurrentDBClientSerializationSettings()
			.UseJsonSerializer(customJsonSerializer);

		// When
		var registry = SchemaRegistry.From(settings);

		// Then
		Assert.Same(customJsonSerializer, registry.GetSerializer(ContentType.Json));
		Assert.NotSame(customJsonSerializer, registry.GetSerializer(ContentType.Bytes));
	}

	[Fact]
	public void From_WithCustomBytesSerializer_UsesProvidedSerializer() {
		// Given
		var customBytesSerializer = new SystemTextJsonSerializer(
			new SystemTextJsonSerializationSettings {
				Options = new JsonSerializerOptions { WriteIndented = true }
			}
		);

		var settings = new KurrentDBClientSerializationSettings()
			.UseBytesSerializer(customBytesSerializer);

		// When
		var registry = SchemaRegistry.From(settings);

		// Then
		Assert.Same(customBytesSerializer, registry.GetSerializer(ContentType.Bytes));
		Assert.NotSame(customBytesSerializer, registry.GetSerializer(ContentType.Json));
	}

	[Fact]
	public void From_WithMessageTypeMap_RegistersTypes() {
		// Given
		var settings = new KurrentDBClientSerializationSettings();
		settings.MessageTypeMapping.Register<TestEvent1>("test-event-1");
		settings.MessageTypeMapping.Register<TestEvent2>("test-event-2");

		// When
		var registry = SchemaRegistry.From(settings);

		// Then
		// Verify types can be resolved
		Assert.True(registry.TryResolveClrType(TestRecord("test-event-1"), out var type1));
		Assert.Equal(typeof(TestEvent1), type1);

		Assert.True(registry.TryResolveClrType(TestRecord("test-event-2"), out var type2));
		Assert.Equal(typeof(TestEvent2), type2);
	}

	[Fact]
	public void From_WithCategoryMessageTypesMap_WithDefaultMessageAutoRegistration() {
		// Given
		var settings                         = new KurrentDBClientSerializationSettings();
		var defaultMessageTypeNamingStrategy = new DefaultMessageTypeNamingStrategy(settings.MessageTypeMapping.DefaultMetadataType);

		// When
		var registry = SchemaRegistry.From(settings);

		// Then
		// For categories, the naming strategy should have resolved the type names
		// using the ResolveTypeName method, which by default uses the type's name
		string typeName1 = registry.ResolveTypeName(
			typeof(TestEvent1),
			new MessageTypeNamingResolutionContext("category1")
		);

		string expectedTypeName1 = defaultMessageTypeNamingStrategy.ResolveTypeName(
			typeof(TestEvent1),
			new MessageTypeNamingResolutionContext("category1")
		);

		Assert.Equal(expectedTypeName1, typeName1);

		string typeName2 = registry.ResolveTypeName(
			typeof(TestEvent2),
			new MessageTypeNamingResolutionContext("category1")
		);

		string expectedTypeName2 = defaultMessageTypeNamingStrategy.ResolveTypeName(
			typeof(TestEvent2),
			new MessageTypeNamingResolutionContext("category1")
		);

		Assert.Equal(expectedTypeName2, typeName2);

		string typeName3 = registry.ResolveTypeName(
			typeof(TestEvent3),
			new MessageTypeNamingResolutionContext("category2")
		);

		string expectedTypeName3 = defaultMessageTypeNamingStrategy.ResolveTypeName(
			typeof(TestEvent3),
			new MessageTypeNamingResolutionContext("category2")
		);

		Assert.Equal(expectedTypeName3, typeName3);

		// Verify types can be resolved by the type names
		Assert.True(registry.TryResolveClrType(TestRecord(typeName1), out var resolvedType1));
		Assert.Equal(typeof(TestEvent1), resolvedType1);

		Assert.True(registry.TryResolveClrType(TestRecord(typeName2), out var resolvedType2));
		Assert.Equal(typeof(TestEvent2), resolvedType2);

		Assert.True(registry.TryResolveClrType(TestRecord(typeName3), out var resolvedType3));
		Assert.Equal(typeof(TestEvent3), resolvedType3);
	}

	[Fact]
	public void From_WithCategoryMessageTypesMap_RegistersTypesWithCategories() {
		// Given
		var settings = new KurrentDBClientSerializationSettings();
		settings.MessageTypeMapping.RegisterForCategory<TestEvent1>("category1");
		settings.MessageTypeMapping.RegisterForCategory<TestEvent2>("category1");
		settings.MessageTypeMapping.RegisterForCategory<TestEvent3>("category2");

		// When
		var registry = SchemaRegistry.From(settings);

		// Then
		// For categories, the naming strategy should have resolved the type names
		// using the ResolveTypeName method, which by default uses the type's name
		string typeName1 = registry.ResolveTypeName(
			typeof(TestEvent1),
			new MessageTypeNamingResolutionContext("category1")
		);

		string typeName2 = registry.ResolveTypeName(
			typeof(TestEvent2),
			new MessageTypeNamingResolutionContext("category1")
		);

		string typeName3 = registry.ResolveTypeName(
			typeof(TestEvent3),
			new MessageTypeNamingResolutionContext("category2")
		);

		// Verify types can be resolved by the type names
		Assert.True(registry.TryResolveClrType(TestRecord(typeName1), out var resolvedType1));
		Assert.Equal(typeof(TestEvent1), resolvedType1);

		Assert.True(registry.TryResolveClrType(TestRecord(typeName2), out var resolvedType2));
		Assert.Equal(typeof(TestEvent2), resolvedType2);

		Assert.True(registry.TryResolveClrType(TestRecord(typeName3), out var resolvedType3));
		Assert.Equal(typeof(TestEvent3), resolvedType3);
	}

	[Fact]
	public void From_WithCustomNamingStrategy_UsesProvidedStrategy() {
		// Given
		var customNamingStrategy = new TestNamingStrategy();
		var settings = new KurrentDBClientSerializationSettings()
			.UseMessageTypeNamingStrategy(customNamingStrategy);

		// When
		var registry = SchemaRegistry.From(settings);

		// Then
		// The registry wraps the naming strategy, but should still use it
		// Test to make sure it behaves like our custom strategy
		string typeName = registry.ResolveTypeName(
			typeof(TestEvent1),
			new MessageTypeNamingResolutionContext("test")
		);

		// Our test strategy adds "Custom-" prefix
		Assert.StartsWith("Custom-", typeName);
	}

	[Fact]
	public void From_WithNoMessageTypeNamingStrategy_UsesDefaultStrategy() {
		// Given
		var settings = new KurrentDBClientSerializationSettings {
			MessageTypeNamingStrategy = null,
		}.ConfigureTypeMap(setting => setting.DefaultMetadataType = typeof(TestMetadata));

		// When
		var registry = SchemaRegistry.From(settings);

		// Then
		// The wrapped default strategy should use our metadata type
		Assert.True(registry.TryResolveClrMetadataType(TestRecord("some-type"), out var defaultMetadataType));

		Assert.Equal(typeof(TestMetadata), defaultMetadataType);
	}
	
	static EventRecord TestRecord(
		string eventType
	) =>
		new(
			Uuid.NewUuid().ToString(),
			Uuid.NewUuid(),
			StreamPosition.FromInt64(0),
			new Position(1, 1),
			new Dictionary<string, string> {
				{ Constants.Metadata.Type, eventType },
				{ Constants.Metadata.Created, DateTime.UtcNow.ToTicksSinceEpoch().ToString() },
				{ Constants.Metadata.ContentType, Constants.Metadata.ContentTypes.ApplicationJson }
			},
			ReadOnlyMemory<byte>.Empty,
			ReadOnlyMemory<byte>.Empty
		);

	// Custom naming strategy for testing
	class TestNamingStrategy : IMessageTypeNamingStrategy {
		public string ResolveTypeName(Type type, MessageTypeNamingResolutionContext context) {
			return $"Custom-{type.Name}-{context.CategoryName}";
		}
#if NET48
	public bool TryResolveClrType(EventRecord record, out Type? clrType)
#else
		public bool TryResolveClrType(EventRecord record, [NotNullWhen(true)] out Type? clrType)
#endif
		{
			// Simple implementation for testing
			clrType = record.EventType.StartsWith("Custom-TestEvent1")
				? typeof(TestEvent1)
				: null;

			return clrType != null;
		}

#if NET48
	public bool TryResolveClrMetadataType(EventRecord record, out Type? clrType)
#else
		public bool TryResolveClrMetadataType(EventRecord record, [NotNullWhen(true)] out Type? clrType)
#endif
		{
			clrType = typeof(TestMetadata);
			return true;
		}
	}
}
