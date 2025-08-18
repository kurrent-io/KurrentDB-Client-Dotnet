using Kurrent.Client.Registry;
using Kurrent.Client.Streams;

namespace Kurrent.Client.Tests.Registry;

public class GetSchemaVersionTests : KurrentClientTestFixture  {
	const int TestTimeoutMs = 20_000;

	[Test, Timeout(TestTimeoutMs)]
	public async Task get_latest_schema_by_name(CancellationToken ct) {
		var schemaName       = NewSchemaName();
        var schemaDefinition = NewJsonSchemaDefinition().ToJson();

        await AutomaticClient.Registry
            .CreateSchema(schemaName, schemaDefinition, SchemaDataFormat.Json, ct)
            .ShouldNotThrowOrFailAsync();

		await AutomaticClient.Registry
			.GetSchemaVersion(schemaName, cancellationToken: ct)
			.ShouldNotThrowOrFailAsync(version => {
				version.VersionId.ShouldBeGuid();
				version.VersionNumber.ShouldBe(1);
				version.SchemaDefinition.ShouldBe(schemaDefinition);
				version.DataFormat.ShouldBe(SchemaDataFormat.Json);
				version.RegisteredAt.ShouldBeGreaterThan(DateTimeOffset.MinValue);
			});
	}

	[Test, Timeout(TestTimeoutMs)]
	public async Task get_schema_by_name_and_version(CancellationToken ct) {
        var schemaName       = NewSchemaName();
        var schemaDefinition = NewJsonSchemaDefinition().ToJson();

		var schemaVersion = await AutomaticClient.Registry
            .CreateSchema(schemaName, schemaDefinition, SchemaDataFormat.Json, ct)
            .ShouldNotThrowOrFailAsync();

		await AutomaticClient.Registry
			.GetSchemaVersion(schemaName, schemaVersion.VersionNumber, cancellationToken: ct)
			.ShouldNotThrowOrFailAsync(version => {
				version.VersionId.ShouldBeGuid();
				version.VersionNumber.ShouldBe(1);
				version.SchemaDefinition.ShouldBe(schemaDefinition);
				version.DataFormat.ShouldBe(SchemaDataFormat.Json);
				version.RegisteredAt.ShouldBeGreaterThan(DateTimeOffset.MinValue);
			});
	}

	[Test, Timeout(TestTimeoutMs)]
	public async Task get_schema_by_id(CancellationToken ct) {
		// Arrange
		var schemaName       = NewSchemaName();
        var schemaDefinition = NewJsonSchemaDefinition().ToJson();

		var schemaVersion = await AutomaticClient.Registry
            .CreateSchema(schemaName, schemaDefinition, SchemaDataFormat.Json, ct)
            .ShouldNotThrowOrFailAsync();

		// Act & Assert
		await AutomaticClient.Registry
			.GetSchemaVersionById(schemaVersion.VersionId, cancellationToken: ct)
            .ShouldNotThrowOrFailAsync(version => {
                version.VersionId.ShouldBeGuid();
                version.VersionNumber.ShouldBe(1);
                version.SchemaDefinition.ShouldBe(schemaDefinition);
                version.DataFormat.ShouldBe(SchemaDataFormat.Json);
                version.RegisteredAt.ShouldBeGreaterThan(DateTimeOffset.MinValue);
            });
	}

	[Test, Timeout(TestTimeoutMs)]
	public async Task get_schema_by_name_fails_on_unknown_name(CancellationToken ct) {
		// Arrange
		var nonExistentSchemaName = NewSchemaName();

		// Act & Assert
		await AutomaticClient.Registry
			.GetSchemaVersion(nonExistentSchemaName, cancellationToken: ct)
			.ShouldFailAsync(failure =>
                failure.Case.ShouldBe(GetSchemaVersionError.GetSchemaVersionErrorCase.NotFound));
	}

	[Test, Timeout(TestTimeoutMs)]
	public async Task get_schema_by_name_fails_on_invalid_version(CancellationToken ct) {
		// Arrange
		var schemaName       = NewSchemaName();
        var schemaDefinition = NewJsonSchemaDefinition().ToJson();

        await AutomaticClient.Registry
            .CreateSchema(schemaName, schemaDefinition, SchemaDataFormat.Json, ct)
            .ShouldNotThrowOrFailAsync();

		const int nonExistentVersionNumber = 999;

		await AutomaticClient.Registry
			.GetSchemaVersion(schemaName, nonExistentVersionNumber, ct)
            .ShouldFailAsync(failure =>
                failure.Case.ShouldBe(GetSchemaVersionError.GetSchemaVersionErrorCase.NotFound));
	}

	[Test, Timeout(TestTimeoutMs)]
	public async Task get_schema_by_id_fails_on_unknown_id(CancellationToken ct) {
		var nonExistentVersionId = SchemaVersionId.From(Guid.NewGuid());

		await AutomaticClient.Registry
			.GetSchemaVersionById(nonExistentVersionId, cancellationToken: ct)
			.ShouldFailAsync(failure =>
                failure.Case.ShouldBe(GetSchemaVersionError.GetSchemaVersionErrorCase.NotFound));
	}
}
