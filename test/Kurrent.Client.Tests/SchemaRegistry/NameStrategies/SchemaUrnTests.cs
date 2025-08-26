using SchemaUrn = Kurrent.Client.Schema.NameStrategies.SchemaUrn;

namespace Kurrent.Client.Tests.SchemaRegistry.NameStrategies;

public class SchemaUrnTests {
	const string ValidNamespace   = "company.department.service";
	const string ValidMessageName = "resource.event";

	static readonly Guid ValidGuid = Guid.Parse("12345678-1234-1234-1234-123456789012");

	[Test]
	public void creates_valid_urn_with_all_components() {
		var urn = SchemaUrn.Create(ValidNamespace, ValidMessageName, ValidGuid);

		urn.Namespace.ShouldBe(ValidNamespace);
		urn.MessageName.ShouldBe(ValidMessageName);
		urn.SchemaGuid.ShouldBe(ValidGuid);
		urn.Value.ShouldBe($"urn:schemas-kurrent:{ValidNamespace}:{ValidMessageName}:{ValidGuid}");
	}

	[Test]
	public void creates_valid_urn_without_guid() {
		var urn = SchemaUrn.Create(ValidNamespace, ValidMessageName);

		urn.Namespace.ShouldBe(ValidNamespace);
		urn.MessageName.ShouldBe(ValidMessageName);
		urn.SchemaGuid.ShouldBeNull();
		urn.Value.ShouldBe($"urn:schemas-kurrent:{ValidNamespace}:{ValidMessageName}");
	}

	[Test]
	public void throws_argument_exception_when_namespace_is_empty() {
		Should.Throw<ArgumentException>(() => SchemaUrn.Create("", ValidMessageName));
	}

	[Test]
	public void throws_argument_exception_when_namespace_is_whitespace() {
		Should.Throw<ArgumentException>(() => SchemaUrn.Create("   ", ValidMessageName));
	}

	[Test]
	public void throws_argument_exception_when_namespace_contains_separator() {
		Should.Throw<ArgumentException>(() => SchemaUrn.Create("company:department", ValidMessageName));
	}

	[Test]
	public void throws_argument_exception_when_message_name_is_empty() {
		Should.Throw<ArgumentException>(() => SchemaUrn.Create(ValidNamespace, ""));
	}

	[Test]
	public void throws_argument_exception_when_message_name_is_whitespace() {
		Should.Throw<ArgumentException>(() => SchemaUrn.Create(ValidNamespace, "   "));
	}

	[Test]
	public void throws_argument_exception_when_message_name_contains_separator() {
		Should.Throw<ArgumentException>(() => SchemaUrn.Create(ValidNamespace, "message:name"));
	}

	[Test]
	public void parse_returns_valid_urn_when_input_has_guid() {
		var urnString = $"urn:schemas-kurrent:{ValidNamespace}:{ValidMessageName}:{ValidGuid}";

		var urn = SchemaUrn.Parse(urnString);

		urn.Namespace.ShouldBe(ValidNamespace);
		urn.MessageName.ShouldBe(ValidMessageName);
		urn.SchemaGuid.ShouldBe(ValidGuid);
		urn.Value.ShouldBe(urnString);
	}

	[Test]
	public void parse_returns_valid_urn_when_input_has_no_guid() {
		var urnString = $"urn:schemas-kurrent:{ValidNamespace}:{ValidMessageName}";

		var urn = SchemaUrn.Parse(urnString);

		urn.Namespace.ShouldBe(ValidNamespace);
		urn.MessageName.ShouldBe(ValidMessageName);
		urn.SchemaGuid.ShouldBeNull();
		urn.Value.ShouldBe(urnString);
	}

	[Test]
	public void parse_throws_argument_exception_when_input_is_empty() {
		Should.Throw<ArgumentException>(() => SchemaUrn.Parse(""));
	}

	[Test]
	public void parse_throws_argument_exception_when_input_is_whitespace() {
		Should.Throw<ArgumentException>(() => SchemaUrn.Parse("   "));
	}

	[Test]
	public void parse_throws_format_exception_when_format_is_invalid() {
		Should.Throw<FormatException>(() => SchemaUrn.Parse("invalid-format"));
	}

	[Test]
	public void parse_throws_format_exception_when_prefix_is_invalid() {
		Should.Throw<FormatException>(() => SchemaUrn.Parse($"urn:wrong-prefix:{ValidNamespace}:{ValidMessageName}"));
	}

	[Test]
	public void parse_throws_format_exception_when_guid_format_is_invalid() {
		Should.Throw<FormatException>(() => SchemaUrn.Parse($"urn:schemas-kurrent:{ValidNamespace}:{ValidMessageName}:not-a-guid"));
	}

	[Test]
	public void try_parse_returns_true_and_urn_when_input_has_guid() {
		var urnString = $"urn:schemas-kurrent:{ValidNamespace}:{ValidMessageName}:{ValidGuid}";

		var success = SchemaUrn.TryParse(urnString, out var urn);

		success.ShouldBeTrue();
		urn.ShouldNotBeNull();
		urn.Namespace.ShouldBe(ValidNamespace);
		urn.MessageName.ShouldBe(ValidMessageName);
		urn.SchemaGuid.ShouldBe(ValidGuid);
		urn.Value.ShouldBe(urnString);
	}

	[Test]
	public void try_parse_returns_true_and_urn_when_input_has_no_guid() {
		var urnString = $"urn:schemas-kurrent:{ValidNamespace}:{ValidMessageName}";

		var success = SchemaUrn.TryParse(urnString, out var urn);

		success.ShouldBeTrue();
		urn.ShouldNotBeNull();
		urn.Namespace.ShouldBe(ValidNamespace);
		urn.MessageName.ShouldBe(ValidMessageName);
		urn.SchemaGuid.ShouldBeNull();
		urn.Value.ShouldBe(urnString);
	}

	[Test]
	public void try_parse_returns_false_when_input_is_empty() {
		var success = SchemaUrn.TryParse("", out var urn);

		success.ShouldBeFalse();
		urn.ShouldBeNull();
	}

	[Test]
	public void try_parse_returns_false_when_format_is_invalid() {
		var success = SchemaUrn.TryParse("invalid-format", out var urn);

		success.ShouldBeFalse();
		urn.ShouldBeNull();
	}

	[Test]
	public void try_parse_returns_false_when_guid_format_is_invalid() {
		var success = SchemaUrn.TryParse($"urn:schemas-kurrent:{ValidNamespace}:{ValidMessageName}:not-a-guid", out var urn);

		success.ShouldBeFalse();
		urn.ShouldBeNull();
	}

	[Test]
	public void to_string_returns_correct_urn_string() {
		var urn = SchemaUrn.Create(ValidNamespace, ValidMessageName, ValidGuid);

		var urnString = urn.ToString();

		urnString.ShouldBe($"urn:schemas-kurrent:{ValidNamespace}:{ValidMessageName}:{ValidGuid}");
	}

	[Test]
	public void implicit_conversion_to_string_returns_correct_urn_string() {
		var urn = SchemaUrn.Create(ValidNamespace, ValidMessageName, ValidGuid);

		string urnString = urn;

		urnString.ShouldBe($"urn:schemas-kurrent:{ValidNamespace}:{ValidMessageName}:{ValidGuid}");
	}

	[Test]
	public void equal_objects_should_be_equal_and_have_same_hash_code() {
		var urn1 = SchemaUrn.Create(ValidNamespace, ValidMessageName, ValidGuid);
		var urn2 = SchemaUrn.Create(ValidNamespace, ValidMessageName, ValidGuid);

		urn1.ShouldBe(urn2);
		urn1.GetHashCode().ShouldBe(urn2.GetHashCode());
	}

	[Test]
	public void objects_with_different_namespaces_should_not_be_equal() {
		var urn1 = SchemaUrn.Create(ValidNamespace, ValidMessageName, ValidGuid);
		var urn2 = SchemaUrn.Create("different.namespace", ValidMessageName, ValidGuid);

		urn1.ShouldNotBe(urn2);
		urn1.GetHashCode().ShouldNotBe(urn2.GetHashCode());
	}

	[Test]
	public void objects_with_different_message_names_should_not_be_equal() {
		var urn1 = SchemaUrn.Create(ValidNamespace, ValidMessageName, ValidGuid);
		var urn2 = SchemaUrn.Create(ValidNamespace, "different.message", ValidGuid);

		urn1.ShouldNotBe(urn2);
		urn1.GetHashCode().ShouldNotBe(urn2.GetHashCode());
	}

	[Test]
	public void objects_with_different_guids_should_not_be_equal() {
		var urn1 = SchemaUrn.Create(ValidNamespace, ValidMessageName, ValidGuid);
		var urn2 = SchemaUrn.Create(ValidNamespace, ValidMessageName, Guid.NewGuid());

		urn1.ShouldNotBe(urn2);
		urn1.GetHashCode().ShouldNotBe(urn2.GetHashCode());
	}

	[Test]
	public void object_with_guid_should_not_equal_object_without_guid() {
		var urn1 = SchemaUrn.Create(ValidNamespace, ValidMessageName, ValidGuid);
		var urn2 = SchemaUrn.Create(ValidNamespace, ValidMessageName);

		urn1.ShouldNotBe(urn2);
		urn1.GetHashCode().ShouldNotBe(urn2.GetHashCode());
	}

	[Test]
	public void parses_real_world_urn_example_correctly() {
		var urnString = "urn:schemas-kurrent:bend-studio.days-gone:dlc.broken-roads";

		var success = SchemaUrn.TryParse(urnString, out var urn);

		success.ShouldBeTrue();
		urn.ShouldNotBeNull();
		urn.Namespace.ShouldBe("bend-studio.days-gone");
		urn.MessageName.ShouldBe("dlc.broken-roads");
		urn.SchemaGuid.ShouldBeNull();
	}

	[Test]
	public void parses_real_world_urn_with_guid_example_correctly() {
		var guid      = Guid.NewGuid();
		var urnString = $"urn:schemas-kurrent:bend-studio.days-gone:dlc.broken-roads:{guid}";

		var success = SchemaUrn.TryParse(urnString, out var urn);

		success.ShouldBeTrue();
		urn.ShouldNotBeNull();
		urn.Namespace.ShouldBe("bend-studio.days-gone");
		urn.MessageName.ShouldBe("dlc.broken-roads");
		urn.SchemaGuid.ShouldBe(guid);
	}
}
