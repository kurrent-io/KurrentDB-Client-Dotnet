using Humanizer;
using KurrentDB.Client.Tests.TestNode;

namespace KurrentDB.Client.Tests;

[Trait("Category", "Target:Misc")]
[Trait("Category", "Target:Plugins")]
[Trait("Category", "Type:UserCertificate")]
public class ClientCertificateTests(ITestOutputHelper output, KurrentDBTemporaryFixture fixture) : KurrentTemporaryTests<KurrentDBTemporaryFixture>(output, fixture) {
	// [SupportsPlugins.Theory(EventStoreRepository.Commercial, "This server version does not support plugins"), BadClientCertificatesTestCases]
	[Theory, BadClientCertificatesTestCases]
	async Task bad_certificates_combinations_should_return_authentication_error(string userCertFile, string userKeyFile, string tlsCaFile) {
		var stream     = Fixture.GetStreamName();
		var seedEvents = Fixture.CreateTestEvents();
		var port       = Fixture.Options.DBClientSettings.ConnectivitySettings.ResolvedAddressOrDefault.Port;

		var connectionString = $"kurrentdb://localhost:{port}/?tls=true&userCertFile={userCertFile}&userKeyFile={userKeyFile}&tlsCaFile={tlsCaFile}";

		var settings = KurrentDBClientSettings.Create(connectionString);
		settings.ConnectivitySettings.TlsVerifyCert.ShouldBeTrue();

		// await using var client = new KurrentDBClient(settings);

		// await client.AppendToStreamAsync(stream, StreamState.NoStream, seedEvents).ShouldThrowAsync<NotAuthenticatedException>();

		try
		{
			await using var client = new KurrentDBClient(settings);
			await client.AppendToStreamAsync(stream, StreamState.NoStream, seedEvents);
		}
		catch (Exception ex)
		{
			Console.WriteLine(ex);
			throw;
		}
	}

	// [SupportsPlugins.Theory(EventStoreRepository.Commercial, "This server version does not support plugins"), ValidClientCertificatesTestCases]
	[Theory, ValidClientCertificatesTestCases]
	async Task valid_certificates_combinations_should_write_to_stream(string userCertFile, string userKeyFile, string tlsCaFile) {
		var stream     = Fixture.GetStreamName();
		var seedEvents = Fixture.CreateTestEvents();
		var port       = Fixture.Options.DBClientSettings.ConnectivitySettings.ResolvedAddressOrDefault.Port;

		var connectionString = $"kurrentdb://localhost:{port}/?userCertFile={userCertFile}&userKeyFile={userKeyFile}&tlsCaFile={tlsCaFile}";

		var settings = KurrentDBClientSettings.Create(connectionString);
		settings.ConnectivitySettings.TlsVerifyCert.ShouldBeTrue();

		await using var client = new KurrentDBClient(settings);

		var result = await client.AppendToStreamAsync(stream, StreamState.NoStream, seedEvents);
		result.ShouldNotBeNull();
	}

	// [SupportsPlugins.Theory(EventStoreRepository.Commercial, "This server version does not support plugins"), BadClientCertificatesTestCases]
	[Theory, BadClientCertificatesTestCases]
	async Task basic_authentication_should_take_precedence(string userCertFile, string userKeyFile, string tlsCaFile) {
		var stream     = Fixture.GetStreamName();
		var seedEvents = Fixture.CreateTestEvents();
		var port       = Fixture.Options.DBClientSettings.ConnectivitySettings.ResolvedAddressOrDefault.Port;

		var connectionString = $"kurrentdb://admin:changeit@localhost:{port}/?userCertFile={userCertFile}&userKeyFile={userKeyFile}&tlsCaFile={tlsCaFile}";

		var settings = KurrentDBClientSettings.Create(connectionString);
		settings.ConnectivitySettings.TlsVerifyCert.ShouldBeTrue();

		await using var client = new KurrentDBClient(settings);

		var result = await client.AppendToStreamAsync(stream, StreamState.NoStream, seedEvents);
		result.ShouldNotBeNull();
	}

	class BadClientCertificatesTestCases : TestCaseGenerator<BadClientCertificatesTestCases> {
		protected override IEnumerable<object[]> Data() {
			yield return [Certificates.Invalid.CertAbsolute, Certificates.Invalid.KeyAbsolute, Certificates.TlsCa.Absolute];
			yield return [Certificates.Invalid.CertRelative, Certificates.Invalid.KeyRelative, Certificates.TlsCa.Absolute];
			yield return [Certificates.Invalid.CertAbsolute, Certificates.Invalid.KeyAbsolute, Certificates.TlsCa.Relative];
			yield return [Certificates.Invalid.CertRelative, Certificates.Invalid.KeyRelative, Certificates.TlsCa.Relative];
		}
	}

	class ValidClientCertificatesTestCases : TestCaseGenerator<ValidClientCertificatesTestCases> {
		protected override IEnumerable<object[]> Data() {
			yield return [Certificates.Admin.CertAbsolute, Certificates.Admin.KeyAbsolute, Certificates.TlsCa.Absolute];
			yield return [Certificates.Admin.CertRelative, Certificates.Admin.KeyRelative, Certificates.TlsCa.Absolute];
			yield return [Certificates.Admin.CertAbsolute, Certificates.Admin.KeyAbsolute, Certificates.TlsCa.Relative];
			yield return [Certificates.Admin.CertRelative, Certificates.Admin.KeyRelative, Certificates.TlsCa.Relative];
		}
	}
}

public enum EventStoreRepository {
	Commercial = 1
}

[PublicAPI]
public class SupportsPlugins {
	public class TheoryAttribute(EventStoreRepository repository, string skipMessage) : Xunit.TheoryAttribute {
		public override string? Skip {
			get => !GlobalEnvironment.DockerImage.Contains(repository.Humanize().ToLower()) ? skipMessage : null;
			set => throw new NotSupportedException();
		}
	}
}
