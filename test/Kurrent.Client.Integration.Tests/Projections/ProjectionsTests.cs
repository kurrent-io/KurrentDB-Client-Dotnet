// ReSharper disable ConvertToConstant.Local
// ReSharper disable InconsistentNaming
// ReSharper disable ClassNeverInstantiated.Local

using Kurrent.Client.Projections;
using Kurrent.Client.Testing.TUnit;

namespace Kurrent.Client.Tests.Projections;

[Category("Projections")]
public class ProjectionsTests : KurrentClientTestFixture {
    static readonly string TestQuery = "fromAll().when({ $init: function (state, ev) { return {}; } });";

	[Test, TestTimeouts.FiveSeconds]
	public async Task creates_projection(CancellationToken ct) {
        ProjectionName projection = NewShortTestID();

		await AutomaticClient.Projections
			.CreateProjection(projection, TestQuery, ProjectionSettings.Default, ct)
			.ShouldNotThrowOrFailAsync();

        var details = await AutomaticClient.Projections.GetProjection(projection, ct)
            .ShouldNotThrowOrFailAsync();

        details.Mode.ShouldBe(ProjectionMode.Continuous);
	}

    [Test, TestTimeouts.FiveSeconds]
    public async Task creates_projection_that_tracks_emitted_streams(CancellationToken ct) {
        ProjectionName projection = NewShortTestID();

        var settings = new ProjectionSettings {
            EmitEnabled         = true,
            TrackEmittedStreams = true
        };

        await AutomaticClient.Projections
            .CreateProjection(projection, TestQuery, settings, ct)
            .ShouldNotThrowOrFailAsync();

        var details = await AutomaticClient.Projections.GetProjection(projection, ct)
            .ShouldNotThrowOrFailAsync();

        details.Mode.ShouldBe(ProjectionMode.Continuous);
    }

    [Test, TestTimeouts.FiveSeconds]
    public async Task enables_projection(CancellationToken ct) {
        ProjectionName projection = NewShortTestID();

        await AutomaticClient.Projections
            .EnableProjection(projection, ct)
            .ShouldNotThrowOrFailAsync();

        var details = await AutomaticClient.Projections.GetProjection(projection, ct)
            .ShouldNotThrowOrFailAsync();

        details.ShouldNotBeNull(); // assert status? wth?
    }

    [Test, TestTimeouts.FiveSeconds]
    public async Task disables_projection(CancellationToken ct) {
        ProjectionName projection = NewShortTestID();

        await AutomaticClient.Projections
            .CreateProjection(projection, TestQuery, ProjectionSettings.Default, ct)
            .ShouldNotThrowOrFailAsync();

        await AutomaticClient.Projections
            .EnableProjection(projection, ct)
            .ShouldNotThrowOrFailAsync();

        await AutomaticClient.Projections
            .DisableProjection(projection, ct)
            .ShouldNotThrowOrFailAsync();

        var details = await AutomaticClient.Projections.GetProjection(projection, ct)
            .ShouldNotThrowOrFailAsync();

        details.ShouldNotBeNull(); // assert status? wth?
    }

    [Test, TestTimeouts.FiveSeconds]
    public async Task resets_projection(CancellationToken ct) {
        ProjectionName projection = NewShortTestID();

        await AutomaticClient.Projections
            .CreateProjection(projection, TestQuery, ProjectionSettings.Default, ct)
            .ShouldNotThrowOrFailAsync();

        await AutomaticClient.Projections
            .EnableProjection(projection, ct)
            .ShouldNotThrowOrFailAsync();

        await AutomaticClient.Projections
            .ResetProjection(projection, ct)
            .ShouldNotThrowOrFailAsync();

        var details = await AutomaticClient.Projections
            .GetProjection(projection, ct)
            .ShouldNotThrowOrFailAsync();

        details.ShouldNotBeNull(); // assert status? wth?
    }

    [Test, TestTimeouts.FiveSeconds]
    public async Task aborts_projection(CancellationToken ct) {
        ProjectionName projection = NewShortTestID();

        await AutomaticClient.Projections
            .CreateProjection(projection, TestQuery, ProjectionSettings.Default, ct)
            .ShouldNotThrowOrFailAsync();

        await AutomaticClient.Projections
            .EnableProjection(projection, ct)
            .ShouldNotThrowOrFailAsync();

        await AutomaticClient.Projections
            .AbortProjection(projection, ct)
            .ShouldNotThrowOrFailAsync();

        var details = await AutomaticClient.Projections.GetProjection(projection, ct)
            .ShouldNotThrowOrFailAsync();

        details.ShouldNotBeNull(); // assert status? wth?
    }

    [Test, TestTimeouts.FiveSeconds]
    public async Task gets_projection(CancellationToken ct) {
        ProjectionName projection = NewShortTestID();

        await AutomaticClient.Projections
            .CreateProjection(projection, TestQuery, ProjectionSettings.Default, ct)
            .ShouldNotThrowOrFailAsync();

        var details = await AutomaticClient.Projections.GetProjection(projection, ct)
            .ShouldNotThrowOrFailAsync();

        details.ShouldNotBeNull();
    }


    public class SystemProjectionsCases : TestCaseGenerator<ProjectionName> {
        protected override IEnumerable<ProjectionName> Data() {
            yield return "$streams";
            yield return "$stream_by_category";
            yield return "$by_category";
            yield return "$by_event_type";
            yield return "$by_correlation_id";
        }
    }

    [Test, TestTimeouts.FiveSeconds, SystemProjectionsCases]
    public async Task gets_system_projections(ProjectionName projection, CancellationToken ct) {
        await AutomaticClient.Projections
            .CreateProjection(projection, TestQuery, ProjectionSettings.Default, ct)
            .ShouldNotThrowOrFailAsync();

        var details = await AutomaticClient.Projections.GetProjection(projection, ct)
            .ShouldNotThrowOrFailAsync();

        details.ShouldNotBeNull();
    }

    [Test, TestTimeouts.FiveSeconds]
    public async Task lists_projections(CancellationToken ct) {
        await AutomaticClient.Projections
            .CreateProjection(NewShortTestID(), TestQuery, ProjectionSettings.Default, ct)
            .ShouldNotThrowOrFailAsync();

        await AutomaticClient.Projections
            .CreateProjection(NewShortTestID(), TestQuery, ProjectionSettings.Default, ct)
            .ShouldNotThrowOrFailAsync();

        var result = await AutomaticClient.Projections.ListProjections(ListProjectionsOptions.Default, ct)
            .ShouldNotThrowOrFailAsync();

        result.Count.ShouldBeGreaterThanOrEqualTo(2);
    }

    [Test, TestTimeouts.FiveSeconds]
	public async Task deletes_projection(CancellationToken ct) {
        ProjectionName projection = NewShortTestID();

        await AutomaticClient.Projections
            .CreateProjection(projection, TestQuery, ProjectionSettings.Default, ct)
            .ShouldNotThrowOrFailAsync();

        await AutomaticClient.Projections.DeleteProjection(projection, DeleteProjectionOptions.Default, ct)
            .ShouldNotThrowOrFailAsync();

        var result = await AutomaticClient.Projections.GetProjection(projection, ct);

        result.IsFailure.ShouldBeTrue();
        result.Error.Value.ShouldBeOfType<ErrorDetails.NotFound>();
        result.Error.ShouldBeAssignableTo<ErrorDetails.NotFound>();
    }

    [Test, TestTimeouts.FiveSeconds]
    public async Task fails_to_create_projection_with_already_existing_name(CancellationToken ct) {
        ProjectionName projection = NewShortTestID();

        await AutomaticClient.Projections
            .CreateProjection(projection, TestQuery, ProjectionSettings.Default, ct)
            .ShouldNotThrowOrFailAsync();

        await AutomaticClient.Projections
            .CreateProjection(projection, TestQuery, ProjectionSettings.Default, ct)
            .ShouldFailAsync(error => {
                error.Error.ShouldBeAssignableTo<ErrorDetails.AlreadyExists>();
            });
    }

    [Test, TestTimeouts.FiveSeconds]
    public async Task fails_to_enable_non_existing_projection(CancellationToken ct) {
        await AutomaticClient.Projections
            .EnableProjection(NewShortTestID(), ct)
            .ShouldFailAsync(error => {
                error.Error.ShouldBeAssignableTo<ErrorDetails.NotFound>();
            });
    }

    [Test, TestTimeouts.FiveSeconds]
    public async Task fails_to_disable_non_existing_projection(CancellationToken ct) {
        await AutomaticClient.Projections
            .DisableProjection(NewShortTestID(), ct)
            .ShouldFailAsync(error => {
                error.Error.ShouldBeAssignableTo<ErrorDetails.NotFound>();
            });
    }

    [Test, TestTimeouts.FiveSeconds]
    public async Task fails_to_abort_non_existing_projection(CancellationToken ct) {
        await AutomaticClient.Projections
            .AbortProjection(NewShortTestID(), ct)
            .ShouldFailAsync(error => {
                error.Error.ShouldBeAssignableTo<ErrorDetails.NotFound>();
            });
    }

    [Test, TestTimeouts.FiveSeconds]
    public async Task fails_to_get_non_existing_projection(CancellationToken ct) {
        await AutomaticClient.Projections
            .GetProjection(NewShortTestID(), ct)
            .ShouldFailAsync(error => {
                error.Error.ShouldBeAssignableTo<ErrorDetails.NotFound>();
            });
    }
}
