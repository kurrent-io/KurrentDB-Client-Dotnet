using Kurrent.Client.SchemaRegistry;
using static Kurrent.Client.SchemaRegistry.SchemaNameOutputFormat;

namespace Kurrent.Client.Tests.SchemaRegistry.NameStrategies;

public class SchemaNameStrategiesTests {
	[Test]
	[Arguments(None, "Amazon.Logistics.Events.OrderShipped")]
	[Arguments(KebabCase, "amazon.logistics.events.order-shipped")]
	[Arguments(SnakeCase, "amazon.logistics.events.order_shipped")]
	[Arguments(Urn, "urn:schemas-kurrent:amazon.logistics.events:order-shipped")]
	public void message_schema_name_strategy_generates_correct_name(SchemaNameOutputFormat format, string streamName) {
		var strategy    = new MessageSchemaNameStrategyWithFormat(format);
		var messageType = typeof(Amazon.Logistics.Events.OrderShipped);

		var result = strategy.GenerateSchemaName(messageType);

		result.Value.ShouldBe(streamName);
	}

	[Test]
	[Arguments(None, "Logistics.OrderShipped")]
	[Arguments(KebabCase, "logistics.order-shipped")]
	[Arguments(SnakeCase, "logistics.order_shipped")]
	[Arguments(Urn, "urn:schemas-kurrent:logistics:order-shipped")]
	public void category_schema_name_strategy_generates_correct_name(SchemaNameOutputFormat format, string expectedName) {
		var strategy    = new CategorySchemaNameStrategy(format);
		var messageType = typeof(Amazon.Logistics.Events.OrderShipped);
		var streamName  = "Logistics-101";

		var result = strategy.GenerateSchemaName(messageType, streamName);

		result.Value.ShouldBe(expectedName);
	}

	[Test]
	[Arguments(None, "com.amazon.logistics.OrderShipped")]
	[Arguments(KebabCase, "com.amazon.logistics.order-shipped")]
	[Arguments(SnakeCase, "com.amazon.logistics.order_shipped")]
	[Arguments(Urn, "urn:schemas-kurrent:com.amazon.logistics:order-shipped")]
	public void namespace_schema_name_strategy_generates_correct_name(SchemaNameOutputFormat format, string expectedName) {
		var strategy    = new NamespaceSchemaNameStrategy("com.amazon.logistics", format);
		var messageType = typeof(Amazon.Logistics.Events.OrderShipped);

		var result = strategy.GenerateSchemaName(messageType);

		result.Value.ShouldBe(expectedName);
	}

	[Test]
	[Arguments(None, "com.amazon.Logistics.OrderShipped")]
	[Arguments(KebabCase, "com.amazon.logistics.order-shipped")]
	[Arguments(SnakeCase, "com.amazon.logistics.order_shipped")]
	[Arguments(Urn, "urn:schemas-kurrent:com.amazon.logistics:order-shipped")]
	public void namespace_category_schema_name_strategy_generates_correct_name(SchemaNameOutputFormat format, string expectedName) {
		var strategy    = new NamespaceCategorySchemaNameStrategy("com.amazon", format);
		var messageType = typeof(Amazon.Logistics.Events.OrderShipped);
		var streamName  = "Logistics-101";

		var result = strategy.GenerateSchemaName(messageType, streamName);

		result.Value.ShouldBe(expectedName);
	}

	[Test]
	public void message_schema_name_strategy_throws_on_null_type() {
		var strategy = new MessageSchemaNameStrategyWithFormat(None);
		Should.Throw<ArgumentNullException>(() => strategy.GenerateSchemaName(null!));
	}

	[Test]
	public void category_schema_name_strategy_throws_on_empty_stream() {
		var strategy = new CategorySchemaNameStrategy();
		var messageType = typeof(Amazon.Logistics.Events.OrderShipped);
		Should.Throw<ArgumentException>(() => strategy.GenerateSchemaName(messageType, " "));
	}

	[Test]
	public void namespace_schema_name_strategy_throws_on_empty_namespace() {
		Should.Throw<ArgumentException>(() => new NamespaceSchemaNameStrategy(" "));
	}

	[Test]
	public void namespace_category_schema_name_strategy_throws_on_empty_namespace_or_stream() {
		Should.Throw<ArgumentException>(() => new NamespaceCategorySchemaNameStrategy(" "));
		var strategy = new NamespaceCategorySchemaNameStrategy("com.amazon");
		var messageType = typeof(Amazon.Logistics.Events.OrderShipped);
		Should.Throw<ArgumentException>(() => strategy.GenerateSchemaName(messageType, " "));
	}
}
