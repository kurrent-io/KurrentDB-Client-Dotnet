using Kurrent.Client.SchemaRegistry;
using static Kurrent.Client.SchemaRegistry.SchemaNameOutputFormat;

namespace Kurrent.Client.Tests.SchemaRegistry.NameStrategies;

public class SchemaNameStrategiesTests {
	# region happy path

	[Test]
	[Arguments(None, "Company.Logistics.Events.OrderShipped")]
	[Arguments(KebabCase, "company.logistics.events.order-shipped")]
	[Arguments(SnakeCase, "company.logistics.events.order_shipped")]
	[Arguments(Urn, "urn:schemas-kurrent:company.logistics.events:order-shipped")]
	public void message_schema_name_strategy_generates_correct_name(SchemaNameOutputFormat format, string streamName) {
		var strategy    = new MessageSchemaNameStrategy(format);
		var messageType = typeof(Company.Logistics.Events.OrderShipped);
		var result      = strategy.GenerateSchemaName(messageType);
		result.Value.ShouldBe(streamName);
	}

	[Test]
	[Arguments(None, "Logistics-101", "Logistics.OrderShipped")]
	[Arguments(None, "Logistics", "Logistics.OrderShipped")]
	[Arguments(KebabCase, "Logistics-101", "logistics.order-shipped")]
	[Arguments(SnakeCase, "Logistics-101", "logistics.order_shipped")]
	[Arguments(Urn, "Logistics-101", "urn:schemas-kurrent:logistics:order-shipped")]
	public void category_schema_name_strategy_generates_correct_name(SchemaNameOutputFormat format, string streamName, string expectedName) {
		var strategy    = new CategorySchemaNameStrategy(format);
		var messageType = typeof(Company.Logistics.Events.OrderShipped);
		var result      = strategy.GenerateSchemaName(messageType, streamName);
		result.Value.ShouldBe(expectedName);
	}

	[Test]
	[Arguments(None, "com.company.logistics.OrderShipped")]
	[Arguments(KebabCase, "com.company.logistics.order-shipped")]
	[Arguments(SnakeCase, "com.company.logistics.order_shipped")]
	[Arguments(Urn, "urn:schemas-kurrent:com.company.logistics:order-shipped")]
	public void namespace_schema_name_strategy_generates_correct_name(SchemaNameOutputFormat format, string expectedName) {
		var strategy    = new NamespaceSchemaNameStrategy("com.company.logistics", format);
		var messageType = typeof(Company.Logistics.Events.OrderShipped);
		var result      = strategy.GenerateSchemaName(messageType);
		result.Value.ShouldBe(expectedName);
	}

	[Test]
	[Arguments(None, "Logistics-101", "com.company.Logistics.OrderShipped")]
	[Arguments(None, "Logistics", "com.company.Logistics.OrderShipped")]
	[Arguments(KebabCase, "Logistics-101", "com.company.logistics.order-shipped")]
	[Arguments(SnakeCase, "Logistics-101", "com.company.logistics.order_shipped")]
	[Arguments(Urn, "Logistics-101", "urn:schemas-kurrent:com.company.logistics:order-shipped")]
	public void namespace_category_schema_name_strategy_generates_correct_name(SchemaNameOutputFormat format, string streamName, string expectedName) {
		var strategy    = new NamespaceCategorySchemaNameStrategy("com.company", format);
		var messageType = typeof(Company.Logistics.Events.OrderShipped);

		var result = strategy.GenerateSchemaName(messageType, streamName);

		result.Value.ShouldBe(expectedName);
	}

	# endregion

	#region edge cases

	[Test]
	public void message_schema_name_strategy_generates_correct_name_with_nested_types() {
		var strategy    = new MessageSchemaNameStrategy();
		var messageType = typeof(Company.Logistics.Events.OrderShipped.Address);
		var result      = strategy.GenerateSchemaName(messageType);
		result.Value.ShouldBe("Company.Logistics.Events.Address");
	}

	[Test]
	public void category_schema_name_strategy_throws_on_empty_stream() {
		var strategy    = new CategorySchemaNameStrategy();
		var messageType = typeof(Company.Logistics.Events.OrderShipped);
		Should.Throw<ArgumentException>(() => strategy.GenerateSchemaName(messageType, " "));
	}

	[Test]
	public void namespace_schema_name_strategy_throws_on_empty_namespace() {
		Should.Throw<ArgumentException>(() => new NamespaceSchemaNameStrategy(" "));
	}

	[Test]
	public void namespace_category_schema_name_strategy_throws_on_empty_namespace_or_stream() {
		Should.Throw<ArgumentException>(() => new NamespaceCategorySchemaNameStrategy(" "));
		var strategy    = new NamespaceCategorySchemaNameStrategy("com.company");
		var messageType = typeof(Company.Logistics.Events.OrderShipped);
		Should.Throw<ArgumentException>(() => strategy.GenerateSchemaName(messageType, " "));
	}

	#endregion
}
