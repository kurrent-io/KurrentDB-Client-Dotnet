using Kurrent.Client.Model;
using Kurrent.Client.SchemaRegistry;
using NJsonSchema;

namespace Kurrent.Client.Tests.Registry;

public class CheckSchemaCompatibilityTests : KurrentClientTestFixture {
	const int TestTimeoutMs = 20_000;

	[Test, Timeout(TestTimeoutMs)]
	public async Task backward_mode_compatible_when_adding_optional_field(CancellationToken ct) {
		// Arrange
		var schemaName = NewSchemaName();

		var v1 = NewJsonSchemaDefinition();
		var v2 = v1.AddOptional("email", JsonObjectType.String);

		var createResult = await AutomaticClient.Registry
			.CreateSchema(schemaName, v1.ToJson(), SchemaDataFormat.Json, CompatibilityMode.Backward, "", new Dictionary<string, string>(), ct)
			.ShouldNotThrowAsync();

		// Act & Assert
		var result = await AutomaticClient.Registry
			.CheckSchemaCompatibility(createResult.Value.VersionId, v2.ToJson(), SchemaDataFormat.Json, ct)
			.ShouldNotThrowAsync();

		result.IsSuccess.ShouldBeTrue();
	}

	[Test, Timeout(TestTimeoutMs)]
	public async Task backward_mode_incompatible_when_deleting_required_field(CancellationToken ct) {
		// Arrange
		var schemaName = NewSchemaName();

		var v1 = NewJsonSchemaDefinition().AddRequired("email", JsonObjectType.String);
		var v2 = v1.Remove("email");

		var createResult = await AutomaticClient.Registry
			.CreateSchema(schemaName, v1.ToJson(), SchemaDataFormat.Json, CompatibilityMode.Backward, "", new Dictionary<string, string>(), ct)
			.ShouldNotThrowAsync();

		// Act & Assert
		var result = await AutomaticClient.Registry
			.CheckSchemaCompatibility(createResult.Value.VersionId, v2.ToJson(), SchemaDataFormat.Json, ct)
			.ShouldNotThrowAsync()
			.OnErrorAsync(error => {
				error.IsSchemaCompatibilityErrors.ShouldBeTrue();
				var errors = error.AsSchemaCompatibilityErrors.Errors;
				errors.ShouldContain(e => e.Kind == SchemaCompatibilityErrorKind.MissingRequiredProperty);
				errors.ShouldContain(e => e.Details.Contains("Required property in original schema is missing in new schema"));
				errors.ShouldContain(e => e.PropertyPath.Contains("email"));
			});

		result.IsSuccess.ShouldBeFalse();
	}

	[Test, Timeout(TestTimeoutMs)]
	public async Task forward_mode_compatible_when_deleting_optional_field(CancellationToken ct) {
		// Arrange
		var schemaName = NewSchemaName();

		var v1 = NewJsonSchemaDefinition().AddOptional("email", JsonObjectType.String);
		var v2 = v1.Remove("email");

		var createResult = await AutomaticClient.Registry
			.CreateSchema(schemaName, v1.ToJson(), SchemaDataFormat.Json, CompatibilityMode.Forward, "", new Dictionary<string, string>(), ct)
			.ShouldNotThrowAsync();

		// Act & Assert
		var result = await AutomaticClient.Registry
			.CheckSchemaCompatibility(createResult.Value.VersionId, v2.ToJson(), SchemaDataFormat.Json, ct)
			.ShouldNotThrowAsync();

		result.IsSuccess.ShouldBeTrue();
	}

	[Test, Timeout(TestTimeoutMs)]
	public async Task forward_mode_incompatible_when_adding_required_field(CancellationToken ct) {
		// Arrange
		var schemaName = NewSchemaName();

		var v1 = NewJsonSchemaDefinition();
		var v2 = v1.AddRequired("email", JsonObjectType.String);

		var createResult = await AutomaticClient.Registry
			.CreateSchema(schemaName, v1.ToJson(), SchemaDataFormat.Json, CompatibilityMode.Forward, "", new Dictionary<string, string>(), ct)
			.ShouldNotThrowAsync();

		// Act & Assert
		var result = await AutomaticClient.Registry
			.CheckSchemaCompatibility(createResult.Value.VersionId, v2.ToJson(), SchemaDataFormat.Json, ct)
			.ShouldNotThrowAsync()
			.OnErrorAsync(error => {
				error.IsSchemaCompatibilityErrors.ShouldBeTrue();
				var errors = error.AsSchemaCompatibilityErrors.Errors;
				errors.ShouldContain(e => e.Kind == SchemaCompatibilityErrorKind.NewRequiredProperty);
				errors.ShouldContain(e => e.Details.Contains("Required property in new schema is missing in original schema"));
				errors.ShouldContain(e => e.PropertyPath.Contains("email"));
			});

		result.IsSuccess.ShouldBeFalse();
	}
}
