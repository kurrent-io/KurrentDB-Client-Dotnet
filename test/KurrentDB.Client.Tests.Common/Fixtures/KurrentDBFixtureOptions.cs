// ReSharper disable InconsistentNaming
// ReSharper disable InvertIf

namespace KurrentDB.Client.Tests;

public record KurrentDBFixtureOptions(
	KurrentDBClientSettings DBClientSettings,
	IDictionary<string, string?> Environment
) {
	public KurrentDBFixtureOptions RunInMemory(bool runInMemory = true) =>
		this with { Environment = Environment.With(x => x["EVENTSTORE_MEM_DB"] = runInMemory.ToString()) };

	public KurrentDBFixtureOptions WithoutDefaultCredentials() => this with { DBClientSettings = DBClientSettings.With(x => x.DefaultCredentials = null) };

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
