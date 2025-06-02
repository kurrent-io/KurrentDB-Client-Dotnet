using Kurrent.Client.Model;
using Kurrent.Client.SchemaRegistry;

namespace Kurrent.Client.Tests;

public class JsonSchemaNetSchemaExporterTests {
	[Test]
	public void works() {
		var exporter2 = new SchemaExporter();

		var schema2 = exporter2.Export(typeof(SchemaVersion), SchemaDataFormat.Json);

	}
}
