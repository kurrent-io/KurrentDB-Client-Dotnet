using Kurrent.Client.Model;
using NJsonSchema;
using ErrorDetails = Kurrent.Client.SchemaRegistry.ErrorDetails;

namespace Kurrent.Client.Tests.Registry;

public class CreateSchemaTests : KurrentClientTestFixture {
	const int TestTimeoutMs = 20_000;

	[Test, Timeout(TestTimeoutMs)]
	public async Task registers_initial_version_of_new_schema(CancellationToken ct) {
		// Arrange
		var schemaName       = NewSchemaName();
		var v1 = NewJsonSchemaDefinition();

		// Act & Assert
		await AutomaticClient.Registry
			.CreateSchema(schemaName, v1.ToJson(), SchemaDataFormat.Json, ct)
			.ShouldNotThrowAsync()
            .OnErrorAsync(error => Should.RecordException(error.ToException()))
			.OnSuccessAsync(async schemaVersionDescriptor => {
				schemaVersionDescriptor.VersionId.ShouldBeGuid();
				schemaVersionDescriptor.VersionNumber.ShouldBe(1);

				await AutomaticClient.Registry
					.GetSchemaVersionById(schemaVersionDescriptor.VersionId, ct)
					.ShouldNotThrowAsync()
					.OnSuccessAsync(schemaVersion => {
						schemaVersion.VersionId.ShouldBe(schemaVersionDescriptor.VersionId);
						schemaVersion.VersionNumber.ShouldBe(1);
						schemaVersion.SchemaDefinition.ShouldBe(v1.ToJson());
						schemaVersion.DataFormat.ShouldBe(SchemaDataFormat.Json);
						schemaVersion.RegisteredAt.ShouldBeGreaterThan(DateTimeOffset.MinValue);
					});
			});
	}

	[Test, Timeout(TestTimeoutMs)]
	public async Task fails_to_create_schema_when_schema_already_exists(CancellationToken ct) {
		// Arrange
		var schemaName       = NewSchemaName();
		var v1 = NewJsonSchemaDefinition();
		var v2 = v1.AddOptional("email", JsonObjectType.String);

		await AutomaticClient.Registry
			.CreateSchema(schemaName, v1.ToJson(), SchemaDataFormat.Json, ct)
			.ShouldNotThrowAsync()
            .OnErrorAsync(error => Should.RecordException(error.ToException()))
			.OnSuccessAsync(async schemaVersionDescriptor => {
				schemaVersionDescriptor.VersionId.ShouldBeGuid();
				schemaVersionDescriptor.VersionNumber.ShouldBe(1);

				await AutomaticClient.Registry
					.GetSchemaVersionById(schemaVersionDescriptor.VersionId, ct)
					.ShouldNotThrowAsync()
					.OnSuccessAsync(schemaVersion => {
						schemaVersion.VersionId.ShouldBe(schemaVersionDescriptor.VersionId);
						schemaVersion.VersionNumber.ShouldBe(1);
						schemaVersion.SchemaDefinition.ShouldBe(v1.ToJson());
						schemaVersion.DataFormat.ShouldBe(SchemaDataFormat.Json);
						schemaVersion.RegisteredAt.ShouldBeGreaterThan(DateTimeOffset.MinValue);
					});
			});

		// Act & Assert
		await AutomaticClient.Registry
			.CreateSchema(schemaName, v2.ToJson(), SchemaDataFormat.Json, ct)
			.ShouldNotThrowAsync()
			.OnErrorAsync(error => {
				var exception = error.ToException().ShouldBeOfType<KurrentClientException>();

				exception.Message.ShouldBe($"Schema '{schemaName}' already exists.");
				exception.ErrorCode.ShouldBe(nameof(ErrorDetails.SchemaAlreadyExists));
			});
	}
}
