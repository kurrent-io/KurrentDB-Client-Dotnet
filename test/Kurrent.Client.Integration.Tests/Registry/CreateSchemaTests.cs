using Kurrent.Client.Registry;
using Kurrent.Client.Streams;

namespace Kurrent.Client.Tests.Registry;

public class CreateSchemaTests : KurrentClientTestFixture {
	const int TestTimeoutMs = 20_000;

	[Test, Timeout(TestTimeoutMs)]
	public async Task registers_initial_version_of_new_schema(CancellationToken ct) {
		var schemaName       = NewSchemaName();
		var schemaDefinition = NewJsonSchemaDefinition().ToJson();

		var schemaVersion = await AutomaticClient.Registry
            .CreateSchema(schemaName, schemaDefinition, SchemaDataFormat.Json, ct)
			.ShouldNotThrowOrFailAsync(version => {
                version.VersionId.ShouldBeGuid();
                version.VersionNumber.ShouldBe(1);
            });

        await AutomaticClient.Registry
            .GetSchemaVersionById(schemaVersion.VersionId, ct)
            .ShouldNotThrowOrFailAsync(version => {
                version.VersionId.ShouldBe(schemaVersion.VersionId);
                version.VersionNumber.ShouldBe(1);
                version.SchemaDefinition.ShouldBe(schemaDefinition);
                version.DataFormat.ShouldBe(SchemaDataFormat.Json);
                version.RegisteredAt.ShouldBeGreaterThan(DateTimeOffset.MinValue);
            });
	}

	[Test, Timeout(TestTimeoutMs)]
	public async Task fails_to_create_schema_when_schema_already_exists(CancellationToken ct) {
        var schemaName       = NewSchemaName();
        var schemaDefinition = NewJsonSchemaDefinition().ToJson();

		await AutomaticClient.Registry
			.CreateSchema(schemaName, schemaDefinition, SchemaDataFormat.Json, ct)
			.ShouldNotThrowOrFailAsync();

		await AutomaticClient.Registry
			.CreateSchema(schemaName, schemaDefinition, SchemaDataFormat.Json, ct)
			.ShouldFailAsync(failure =>
                failure.Case.ShouldBe(CreateSchemaError.CreateSchemaErrorCase.AlreadyExists));
	}
}
