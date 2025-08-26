// ReSharper disable InconsistentNaming
// ReSharper disable InvertIf

namespace KurrentDB.Client.Tests;

public record KurrentDBFixtureOptions(
	KurrentDBClientSettings DBClientSettings,
	IDictionary<string, string?> Environment
) {
	public KurrentDBFixtureOptions RunInMemory(bool runInMemory = true) =>
		this with { Environment = Environment.With(x => x["EVENTSTORE_MEM_DB"] = runInMemory.ToString()) };

	// public KurrentDBFixtureOptions WithoutDefaultCredentials() => this with { DBClientSettings = DBClientSettings.With(x => x.DefaultCredentials = null) };
	// TODO: Clean up the tests to remove because the default credentials are not used in the tests. For now we can use this to avoid passing credentials to the operations.
	public KurrentDBFixtureOptions WithoutDefaultCredentials() => this with {};

	public KurrentDBFixtureOptions RunProjections(bool runProjections = true) =>
		this with {
			Environment = Environment.With(x => {
					x["EVENTSTORE_START_STANDARD_PROJECTIONS"] = runProjections.ToString();
					x["EVENTSTORE_RUN_PROJECTIONS"]            = runProjections ? "All" : "None";
				}
			)
		};
}

public delegate KurrentDBFixtureOptions ConfigureFixture(KurrentDBFixtureOptions options);
