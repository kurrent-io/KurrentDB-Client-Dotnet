using KurrentDB.Client.Core.Serialization;

namespace KurrentDB.Client.Tests.Core.Serialization;

public class MessageTypeRegistryTests {
	[Fact]
	public void Register_StoresTypeAndTypeName() {
		// Given
		var          registry = new MessageTypeRegistry();
		var          type     = typeof(TestEvent1);
		const string typeName = "test-event-1";

		// When
		registry.Register(typeName, type);

		// Then
		Assert.Equal(typeName, registry.GetTypeName(type));
		Assert.Equal(type, registry.GetClrType(typeName));
	}

	[Fact]
	public void Register_CalledTwiceForTheSameTypeOverridesExistingTypeRegistration() {
		// Given
		var          registry         = new MessageTypeRegistry();
		var          type             = typeof(TestEvent1);
		const string originalTypeName = "original-name";
		const string updatedTypeName  = "updated-name";

		// When
		registry.Register(originalTypeName, type);
		registry.Register(updatedTypeName, type);

		// Then
		Assert.Equal(updatedTypeName, registry.GetTypeName(type));
		Assert.Equal(type, registry.GetClrType(updatedTypeName));
		Assert.Equal(type, registry.GetClrType(originalTypeName));
	}

	[Fact]
	public void GetTypeName_ReturnsNullForNotRegisteredType() {
		// Given
		var registry         = new MessageTypeRegistry();
		var unregisteredType = typeof(TestEvent2);

		// When
		var result = registry.GetTypeName(unregisteredType);

		// Then
		Assert.Null(result);
	}

	[Fact]
	public void GetClrType_ReturnsNullForNotRegisteredTypeName() {
		// Given
		var          registry             = new MessageTypeRegistry();
		const string unregisteredTypeName = "unregistered-type";

		// When
		var result = registry.GetClrType(unregisteredTypeName);

		// Then
		Assert.Null(result);
	}

	[Fact]
	public void GetOrAddTypeName_ReturnsExistingTypeName() {
		// Given
		var          registry         = new MessageTypeRegistry();
		var          type             = typeof(TestEvent1);
		const string existingTypeName = "existing-type-name";

		registry.Register(existingTypeName, type);
		var typeResolutionCount = 0;

		// When
		var result = registry.GetOrAddTypeName(
			type,
			_ => {
				typeResolutionCount++;
				return "factory-type-name";
			}
		);

		// Then
		Assert.Equal(existingTypeName, result);
		Assert.Equal(0, typeResolutionCount);
	}

	[Fact]
	public void GetOrAddTypeName_ForNotRegisteredTypeNameAddsNewTypeName() {
		// Given
		var          registry            = new MessageTypeRegistry();
		var          type                = typeof(TestEvent1);
		const string newTypeName         = "new-type-name";
		var          typeResolutionCount = 0;

		// When
		var result = registry.GetOrAddTypeName(
			type,
			_ => {
				typeResolutionCount++;
				return newTypeName;
			}
		);

		// Then
		Assert.Equal(newTypeName, result);
		Assert.Equal(1, typeResolutionCount);
		Assert.Equal(newTypeName, registry.GetTypeName(type));
		Assert.Equal(type, registry.GetClrType(newTypeName));
	}

	[Fact]
	public void GetOrAddClrType_ReturnsExistingClrType() {
		// Given
		var          registry = new MessageTypeRegistry();
		var          type     = typeof(TestEvent1);
		const string typeName = "test-event-name";
		registry.Register(typeName, type);
		var typeResolutionCount = 0;

		// When
		var result = registry.GetOrAddClrType(
			typeName,
			_ => {
				typeResolutionCount++;
				return typeof(TestEvent2);
			}
		);

		// Then
		Assert.Equal(type, result);
		Assert.Equal(0, typeResolutionCount);
	}

	[Fact]
	public void GetOrAddClrType_ForNotExistingTypeAddsNewClrType() {
		// Given
		var          registry            = new MessageTypeRegistry();
		const string typeName            = "test-event-name";
		var          type                = typeof(TestEvent1);
		var          typeResolutionCount = 0;

		// When
		var result = registry.GetOrAddClrType(
			typeName,
			_ => {
				typeResolutionCount++;
				return type;
			}
		);

		// Then
		Assert.Equal(type, result);
		Assert.Equal(1, typeResolutionCount);
		Assert.Equal(typeName, registry.GetTypeName(type));
		Assert.Equal(type, registry.GetClrType(typeName));
	}

	[Fact]
	public void GetOrAddClrType_HandlesNullReturnFromTypeResolution() {
		// Given
		var          registry = new MessageTypeRegistry();
		const string typeName = "unknown-type-name";

		// When
		var result = registry.GetOrAddClrType(typeName, _ => null);

		// Then
		Assert.Null(result);
		Assert.Null(registry.GetClrType(typeName));
	}
	
	
	[Fact]
	public void RegisterGeneric_RegistersTypeWithTypeName() {
		// Given
		var          registry = new MessageTypeRegistry();
		const string typeName = "test-event-1";

		// When
		registry.Register<TestEvent1>(typeName);

		// Then
		Assert.Equal(typeName, registry.GetTypeName(typeof(TestEvent1)));
		Assert.Equal(typeof(TestEvent1), registry.GetClrType(typeName));
	}

	[Fact]
	public void RegisterDictionary_RegistersMultipleTypes() {
		// Given
		var registry = new MessageTypeRegistry();
		var typeMap = new Dictionary<string, Type> {
			{ "test-event-1", typeof(TestEvent1) },
			{ "test-event-2", typeof(TestEvent2) }
		};

		// When
		registry.Register(typeMap);

		// Then
		Assert.Equal("test-event-1", registry.GetTypeName(typeof(TestEvent1)));
		Assert.Equal("test-event-2", registry.GetTypeName(typeof(TestEvent2)));
		Assert.Equal(typeof(TestEvent1), registry.GetClrType("test-event-1"));
		Assert.Equal(typeof(TestEvent2), registry.GetClrType("test-event-2"));
	}

	[Fact]
	public void GetTypeNameGeneric_ReturnsTypeName() {
		// Given
		var          registry = new MessageTypeRegistry();
		const string typeName = "test-event-1";
		registry.Register<TestEvent1>(typeName);

		// When
		var result = registry.GetTypeName<TestEvent1>();

		// Then
		Assert.Equal(typeName, result);
	}

	[Fact]
	public void GetTypeNameGeneric_ReturnsNullForUnregisteredType() {
		// Given
		var registry = new MessageTypeRegistry();

		// When
		var result = registry.GetTypeName<TestEvent2>();

		// Then
		Assert.Null(result);
	}

	record TestEvent1;

	record TestEvent2;
}
