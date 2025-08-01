using Kurrent.Client.Model;
using Kurrent.Client.Registry;
using Kurrent.Client.Streams;

namespace Kurrent.Client.Tests.Registry;

public class GetSchemaVersionTests : KurrentClientTestFixture  {
	const int TestTimeoutMs = 20_000;

	[Test, Timeout(TestTimeoutMs)]
	public async Task get_latest_schema_by_name(CancellationToken ct) {
		// Arrange
		var schemaName = NewSchemaName();
		var v1 = NewJsonSchemaDefinition();

		await AutomaticClient.Registry.CreateSchema(schemaName, v1.ToJson(), SchemaDataFormat.Json, ct);

		// Act & Assert
		await AutomaticClient.Registry
			.GetSchemaVersion(schemaName, cancellationToken: ct)
			.ShouldNotThrowAsync()
			.OnFailureAsync(failure => KurrentClientException.Throw(failure))
			.OnSuccessAsync(schemaVersion => {
				schemaVersion.VersionId.ShouldBeGuid();
				schemaVersion.VersionNumber.ShouldBe(1);
				schemaVersion.SchemaDefinition.ShouldBe(v1.ToJson());
				schemaVersion.DataFormat.ShouldBe(SchemaDataFormat.Json);
				schemaVersion.RegisteredAt.ShouldBeGreaterThan(DateTimeOffset.MinValue);
			});
	}

	[Test, Timeout(TestTimeoutMs)]
	public async Task get_schema_by_name_and_version(CancellationToken ct) {
		// Arrange
		var schemaName = NewSchemaName();
		var v1 = NewJsonSchemaDefinition();

		var createResult = await AutomaticClient.Registry.CreateSchema(schemaName, v1.ToJson(), SchemaDataFormat.Json, ct);

		// Act & Assert
		await AutomaticClient.Registry
			.GetSchemaVersion(schemaName, createResult.Value.VersionNumber, cancellationToken: ct)
			.ShouldNotThrowAsync()
			.OnFailureAsync(failure => KurrentClientException.Throw(failure))
			.OnSuccessAsync(schemaVersion => {
				schemaVersion.VersionId.ShouldBeGuid();
				schemaVersion.VersionNumber.ShouldBe(1);
				schemaVersion.SchemaDefinition.ShouldBe(v1.ToJson());
				schemaVersion.DataFormat.ShouldBe(SchemaDataFormat.Json);
				schemaVersion.RegisteredAt.ShouldBeGreaterThan(DateTimeOffset.MinValue);
			});
	}

	[Test, Timeout(TestTimeoutMs)]
	public async Task get_schema_by_id(CancellationToken ct) {
		// Arrange
		var schemaName = NewSchemaName();
		var v1 = NewJsonSchemaDefinition();

		var createResult = await AutomaticClient.Registry.CreateSchema(schemaName, v1.ToJson(), SchemaDataFormat.Json, ct);

		// Act & Assert
		await AutomaticClient.Registry
			.GetSchemaVersionById(createResult.Value.VersionId, cancellationToken: ct)
			.ShouldNotThrowAsync()
			.OnFailureAsync(failure => KurrentClientException.Throw(failure))
			.OnSuccessAsync(schemaVersion => {
				schemaVersion.VersionId.ShouldBeGuid();
				schemaVersion.VersionNumber.ShouldBe(1);
				schemaVersion.SchemaDefinition.ShouldBe(v1.ToJson());
				schemaVersion.DataFormat.ShouldBe(SchemaDataFormat.Json);
				schemaVersion.RegisteredAt.ShouldBeGreaterThan(DateTimeOffset.MinValue);
			});
	}

	[Test, Timeout(TestTimeoutMs)]
	public async Task get_schema_by_name_fails_on_unknown_name(CancellationToken ct) {
		// Arrange
		var nonExistentSchemaName = NewSchemaName();

		// Act & Assert
		await AutomaticClient.Registry
			.GetSchemaVersion(nonExistentSchemaName, cancellationToken: ct)
			.ShouldNotThrowAsync()
			.OnSuccessAsync(schemaVersion => KurrentClientException.Throw($"Expected an error, but got a schema version {schemaVersion}"))
			.OnFailureAsync(failure => {
				failure.IsSchemaNotFound.ShouldBeTrue();
				failure.AsSchemaNotFound.ErrorCode.ShouldBe(nameof(ErrorDetails.SchemaNotFound));
				failure.AsSchemaNotFound.ErrorMessage.ShouldBe($"Schema '{nonExistentSchemaName}' not found.");
			});
	}

	[Test, Timeout(TestTimeoutMs)]
	public async Task get_schema_by_name_fails_on_invalid_version(CancellationToken ct) {
		// Arrange
		var schemaName = NewSchemaName();
		var schema = NewJsonSchemaDefinition();

		await AutomaticClient.Registry
			.CreateSchema(schemaName, schema.ToJson(), SchemaDataFormat.Json, ct)
			.ShouldNotThrowAsync();

		const int nonExistentVersionNumber = 999;

		// Act & Assert
		await AutomaticClient.Registry
			.GetSchemaVersion(schemaName, nonExistentVersionNumber, cancellationToken: ct)
			.ShouldNotThrowAsync()
			.OnSuccessAsync(schemaVersion => KurrentClientException.Throw($"Expected an error, but got a schema version {schemaVersion}"))
			.OnFailureAsync(failure => {
				failure.IsSchemaNotFound.ShouldBeTrue();
				failure.AsSchemaNotFound.ErrorCode.ShouldBe(nameof(ErrorDetails.SchemaNotFound));
				failure.AsSchemaNotFound.ErrorMessage.ShouldBe($"Schema '{schemaName}' not found.");
			});
	}

	[Test, Timeout(TestTimeoutMs)]
	public async Task get_schema_by_id_fails_on_unknown_id(CancellationToken ct) {
		// Arrange
		var nonExistentVersionId = SchemaVersionId.From(Guid.NewGuid());

		// Act & Assert
		await AutomaticClient.Registry
			.GetSchemaVersionById(nonExistentVersionId, cancellationToken: ct)
			.ShouldNotThrowAsync()
			.OnSuccessAsync(schemaVersion => KurrentClientException.Throw($"Expected an error, but got a schema version {schemaVersion}"))
			.OnFailureAsync(failure => {
				failure.IsSchemaNotFound.ShouldBeTrue();
				failure.AsSchemaNotFound.ErrorCode.ShouldBe(nameof(ErrorDetails.SchemaNotFound));
				failure.AsSchemaNotFound.ErrorMessage.ShouldBe($"Schema version '{nonExistentVersionId}' not found.");
			});
	}
}
