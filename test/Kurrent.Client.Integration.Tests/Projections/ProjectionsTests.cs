// ReSharper disable ConvertToConstant.Local
// ReSharper disable InconsistentNaming
// ReSharper disable ClassNeverInstantiated.Local

using Kurrent.Client.Projections;
using Kurrent.Client.Testing.TUnit;
using Microsoft.Extensions.Logging;

namespace Kurrent.Client.Tests.Projections;

[Category("Projections")]
public class ProjectionsTests : KurrentClientTestFixture {
    static readonly string TestQuery = "fromAll().when({ $init: function (state, ev) { return {}; } });";

    async ValueTask TryCleanUpProjection(ProjectionName projection, CancellationToken ct) {
        await AutomaticClient.Projections.DisableProjection(projection, ct);
        await AutomaticClient.Projections.DeleteProjection(projection, ct);
    }

    [Test, TestTimeouts.SixtySeconds]
	public async Task creates_projection(CancellationToken ct) {
        ProjectionName projection = NewShortTestID();

		await AutomaticClient.Projections
			.CreateProjection(projection, TestQuery, ProjectionSettings.Default, ct)
			.ShouldNotThrowOrFailAsync();

        var details = await AutomaticClient.Projections
            .GetProjection(projection, ct)
            .ShouldNotThrowOrFailAsync();

        details.Mode.ShouldBe(ProjectionMode.Continuous);

        Logger.LogInformation("{@ProjectionDetails}", details);
	}

    [Test, TestTimeouts.SixtySeconds]
    public async Task creates_projection_that_tracks_emitted_streams(CancellationToken ct) {
        ProjectionName projection = NewShortTestID();

        var settings = new ProjectionSettings {
            EmitEnabled         = true,
            TrackEmittedStreams = true
        };

        await AutomaticClient.Projections
            .CreateProjection(projection, TestQuery, settings, ct)
            .ShouldNotThrowOrFailAsync();

        var details = await AutomaticClient.Projections
            .GetProjection(projection, ct)
            .ShouldNotThrowOrFailAsync();

        details.Mode.ShouldBe(ProjectionMode.Continuous);

        Logger.LogInformation("{@ProjectionDetails}", details);
    }

    [Test, TestTimeouts.SixtySeconds]
    public async Task deletes_projection(CancellationToken ct) {
        ProjectionName projection = NewShortTestID();

        await AutomaticClient.Projections
            .CreateProjection(projection, TestQuery, ProjectionSettings.Default, ct)
            .ShouldNotThrowOrFailAsync();

        await AutomaticClient.Projections
            .DisableProjection(projection, ct)
            .ShouldNotThrowOrFailAsync();

        await AutomaticClient.Projections
            .DeleteProjection(projection, DeleteProjectionOptions.Default, ct)
            .ShouldNotThrowOrFailAsync();

        var result = await AutomaticClient.Projections
            .GetProjection(projection, ct)
            .ShouldNotThrowAsync();

        result.Error.Case.ShouldBe(GetProjectionError.GetProjectionErrorCase.NotFound, "Projection should not exist after deletion");
    }

    [Test, TestTimeouts.SixtySeconds]
    public async Task enables_projection(CancellationToken ct) {
        ProjectionName projection = NewShortTestID();

        await AutomaticClient.Projections
            .CreateProjection(projection, TestQuery, ProjectionSettings.Default, ct)
            .ShouldNotThrowOrFailAsync();

        await AutomaticClient.Projections
            .EnableProjection(projection, ct)
            .ShouldNotThrowOrFailAsync();

        var details = await AutomaticClient.Projections
            .GetProjection(projection, ct)
            .ShouldNotThrowOrFailAsync();

        Logger.LogInformation("{@ProjectionDetails}", details);

        details.ShouldNotBeNull();
    }

    [Test, TestTimeouts.SixtySeconds]
    public async Task enables_already_enabled_projection(CancellationToken ct) {
        ProjectionName projection = NewShortTestID();

        await AutomaticClient.Projections
            .CreateProjection(projection, TestQuery, ProjectionSettings.Default, ct)
            .ShouldNotThrowOrFailAsync();

        await AutomaticClient.Projections
            .EnableProjection(projection, ct)
            .ShouldNotThrowOrFailAsync();

        await AutomaticClient.Projections
            .EnableProjection(projection, ct)
            .ShouldNotThrowOrFailAsync();

        var details = await AutomaticClient.Projections
            .GetProjection(projection, ct)
            .ShouldNotThrowOrFailAsync();

        Logger.LogInformation("{@ProjectionDetails}", details);

        details.ShouldNotBeNull();
    }

    [Test, TestTimeouts.SixtySeconds]
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

        var details = await AutomaticClient.Projections
            .GetProjection(projection, ct)
            .ShouldNotThrowOrFailAsync();

        Logger.LogInformation("{@ProjectionDetails}", details);

        details.ShouldNotBeNull();
    }

    [Test, TestTimeouts.SixtySeconds]
    public async Task disables_already_disabled_projection(CancellationToken ct) {
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

        await AutomaticClient.Projections
            .DisableProjection(projection, ct)
            .ShouldNotThrowOrFailAsync();

        var details = await AutomaticClient.Projections
            .GetProjection(projection, ct)
            .ShouldNotThrowOrFailAsync();

        Logger.LogInformation("{@ProjectionDetails}", details);

        details.ShouldNotBeNull();
    }

    [Test, TestTimeouts.SixtySeconds]
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

        Logger.LogInformation("{@ProjectionDetails}", details);

        details.ShouldNotBeNull();
    }

    [Test, TestTimeouts.SixtySeconds]
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

        var details = await AutomaticClient.Projections
            .GetProjection(projection, ct)
            .ShouldNotThrowOrFailAsync();

        Logger.LogInformation("{@ProjectionDetails}", details);

        details.ShouldNotBeNull();
    }

    [Test, TestTimeouts.SixtySeconds]
    public async Task gets_projection(CancellationToken ct) {
        ProjectionName projection = NewShortTestID();

        await AutomaticClient.Projections
            .CreateProjection(projection, TestQuery, ProjectionSettings.Default, ct)
            .ShouldNotThrowOrFailAsync();

        var details = await AutomaticClient.Projections
            .GetProjection(projection, ct)
            .ShouldNotThrowOrFailAsync();

        Logger.LogInformation("{@ProjectionDetails}", details);

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

    [Test, TestTimeouts.SixtySeconds, SystemProjectionsCases]
    public async Task gets_system_projections(ProjectionName projection, CancellationToken ct) {
        var details = await AutomaticClient.Projections
            .GetProjection(projection, ct)
            .ShouldNotThrowOrFailAsync();

        details.ShouldNotBeNull();

        details.Mode.ShouldBe(ProjectionMode.Continuous);

        Logger.LogInformation("{@ProjectionDetails}", details);
    }

    [Test, TestTimeouts.SixtySeconds]
    public async Task lists_projections(CancellationToken ct) {
        await AutomaticClient.Projections
            .CreateProjection(NewShortTestID(), TestQuery, ProjectionSettings.Default, ct)
            .ShouldNotThrowOrFailAsync();

        await AutomaticClient.Projections
            .CreateProjection(NewShortTestID(), TestQuery, ProjectionSettings.Default, ct)
            .ShouldNotThrowOrFailAsync();

        var result = await AutomaticClient.Projections
            .ListProjections(ListProjectionsOptions.Default, ct)
            .ShouldNotThrowAsync();

        result.IsSuccess.ShouldBeTrue();
        result.Value.Count.ShouldBeGreaterThanOrEqualTo(2);
    }

    [Test, TestTimeouts.SixtySeconds]
    public async Task fails_to_create_projection_with_already_existing_name(CancellationToken ct) {
        ProjectionName projection = NewShortTestID();

        await AutomaticClient.Projections
            .CreateProjection(projection, TestQuery, ProjectionSettings.Default, ct)
            .ShouldNotThrowOrFailAsync();

        await AutomaticClient.Projections
            .CreateProjection(projection, TestQuery, ProjectionSettings.Default, ct)
            .ShouldFailAsync(error =>
                error.Case.ShouldBe(CreateProjectionError.CreateProjectionErrorCase.AlreadyExists));
    }

    [Test, TestTimeouts.SixtySeconds]
    public async Task fails_to_enable_non_existing_projection(CancellationToken ct) {
        await AutomaticClient.Projections
            .EnableProjection(NewShortTestID(), ct)
            .ShouldFailAsync(error =>
                error.Case.ShouldBe(EnableProjectionError.EnableProjectionErrorCase.NotFound));
    }

    [Test, TestTimeouts.FiveSeconds]
    public async Task fails_to_disable_non_existing_projection(CancellationToken ct) {
        await AutomaticClient.Projections
            .DisableProjection(NewShortTestID(), ct)
            .ShouldFailAsync(error =>
                error.Case.ShouldBe(DisableProjectionError.DisableProjectionErrorCase.NotFound));
    }

    [Test, TestTimeouts.SixtySeconds]
    public async Task fails_to_delete_non_existing_projection(CancellationToken ct) {
        ProjectionName projection = NewShortTestID();

        await AutomaticClient.Projections
            .DeleteProjection(projection, DeleteProjectionOptions.Default, ct)
            .ShouldFailAsync(error =>
                error.Case.ShouldBe(DeleteProjectionError.DeleteProjectionErrorCase.NotFound));
    }

    [Test, TestTimeouts.SixtySeconds]
    public async Task fails_to_delete_running_projection(CancellationToken ct) {
        ProjectionName projection = NewShortTestID();

        await AutomaticClient.Projections
            .CreateProjection(projection, TestQuery, ProjectionSettings.Default, ct)
            .ShouldNotThrowOrFailAsync();

        await AutomaticClient.Projections
            .DeleteProjection(projection, DeleteProjectionOptions.Default, ct)
            .ShouldFailAsync(error =>
                error.Case.ShouldBe(DeleteProjectionError.DeleteProjectionErrorCase.FailedPrecondition));
    }

    [Test, TestTimeouts.SixtySeconds]
    public async Task fails_to_reset_non_existing_projection(CancellationToken ct) {
        await AutomaticClient.Projections
            .ResetProjection(NewShortTestID(), ct)
            .ShouldFailAsync(error =>
                error.Case.ShouldBe(ResetProjectionError.ResetProjectionErrorCase.NotFound));
    }

    [Test, TestTimeouts.SixtySeconds]
    public async Task fails_to_abort_non_existing_projection(CancellationToken ct) {
        await AutomaticClient.Projections
            .AbortProjection(NewShortTestID(), ct)
            .ShouldFailAsync(error =>
                error.Case.ShouldBe(AbortProjectionError.AbortProjectionErrorCase.NotFound));
    }

    [Test, TestTimeouts.SixtySeconds]
    public async Task fails_to_get_non_existing_projection(CancellationToken ct) {
        await AutomaticClient.Projections
            .GetProjection(NewShortTestID(), ct)
            .ShouldFailAsync(error =>
                error.Case.ShouldBe(GetProjectionError.GetProjectionErrorCase.NotFound));
    }
}
