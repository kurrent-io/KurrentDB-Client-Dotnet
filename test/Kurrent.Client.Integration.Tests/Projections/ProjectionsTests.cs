#pragma warning disable TUnit0038 // No data source provided

// ReSharper disable ConvertToConstant.Local
// ReSharper disable InconsistentNaming
// ReSharper disable ClassNeverInstantiated.Local

using EnumerableAsyncProcessor.Extensions;
using Kurrent.Client.Projections;
using Kurrent.Client.Testing.TUnit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Kurrent.Client.Tests.Projections;

[Category("Projections")]
public class ProjectionsTests : KurrentClientTestFixture {
    static readonly string TestDefinition = "fromAll().when({ $init: function (state, ev) { return {}; } });";

    static readonly string AnotherTestDefinition = """
	fromAll().when({
		"$init": function() { return { Count: 0 }; },
		"$any": function(s, e) { 
			log(JSON.stringify(e));
			s.Count++; 
			return s; 
		}
	})
	.outputState();
	""";

    public override ValueTask OnSetUp(IServiceCollection services) => CleanUpProjections();

    async ValueTask CleanUpProjections() {
        var projections = await AutomaticClient.Projections
            .ListAsync(new ListProjectionsOptions { Type = ProjectionType.User })
            .ConfigureAwait(false);

        await using var processor = projections
            .ForEachAsync(async p => {
                if (p.Status == ProjectionStatus.Stopped)
                    Logger.LogInformation("[CleanUpProjections] Projection {ProjectionName} is already stopped: {ProjectionStatus}", p.Name, p.Status);
                else {
                    try {
                        await AutomaticClient.Projections.DisableAsync(p.Name).ConfigureAwait(false);
                        Logger.LogInformation("[CleanUpProjections] Stopped projection {ProjectionName}", p.Name);
                    }
                    catch (Exception ex) {
                        Logger.LogWarning(ex, "[CleanUpProjections] Failed to stop projection {ProjectionName}", p.Name);
                    }
                }

                try {
                    await AutomaticClient.Projections.DeleteAsync(p.Name).ConfigureAwait(false);
                    Logger.LogInformation("[CleanUpProjections] Deleted projection {ProjectionName}", p.Name);
                }
                catch (Exception ex) {
                    Logger.LogWarning(ex, "[CleanUpProjections] Failed to delete projection {ProjectionName}", p.Name);
                }
            })
            .ProcessInParallel();
    }

    [Test, TimeoutAfter.SixtySeconds]
	public async Task creates_projection(CancellationToken ct) {
        ProjectionName projection = NewEntityID();

		await AutomaticClient.Projections
			.Create(projection, TestDefinition, ProjectionSettings.Default, false, ct)
			.ShouldNotThrowOrFailAsync();

        await AutomaticClient.Projections
            .GetDetails(projection, ct)
            .ShouldNotThrowOrFailAsync(details => {
                Logger.LogInformation("{@ProjectionDetails}", details);
                details.Mode.ShouldBe(ProjectionMode.Continuous);
                details.Settings.EmitEnabled.ShouldBeTrue();
                details.Settings.TrackEmittedStreams.ShouldBeFalse();
            });
	}

    [Test, TimeoutAfter.SixtySeconds]
    public async Task creates_projection_that_tracks_emitted_streams(CancellationToken ct) {
        ProjectionName projection = NewEntityID();

        var settings = new ProjectionSettings {
            EmitEnabled         = true,
            TrackEmittedStreams = true,
        };

        await AutomaticClient.Projections
            .Create(projection, TestDefinition, settings, ct)
            .ShouldNotThrowOrFailAsync();

        await AutomaticClient.Projections
            .GetDetails(projection, ct)
            .ShouldNotThrowOrFailAsync(details => {
                Logger.LogInformation("{@ProjectionDetails}", details);
                details.Mode.ShouldBe(ProjectionMode.Continuous);
                details.Settings.EmitEnabled.ShouldBeTrue();
                details.Settings.TrackEmittedStreams.ShouldBeTrue();
            });
    }

    [Test, TimeoutAfter.SixtySeconds]
    public async Task updates_projection_definition(CancellationToken ct) {
        ProjectionName projection = NewEntityID();

        await AutomaticClient.Projections
            .Create(projection, TestDefinition, ProjectionSettings.Default, ct)
            .ShouldNotThrowOrFailAsync();

        await AutomaticClient.Projections
            .UpdateDefinition(projection, AnotherTestDefinition, ct)
            .ShouldNotThrowOrFailAsync();

        await AutomaticClient.Projections
            .GetDetails(projection, new() { IncludeDefinition = true }, ct)
            .ShouldNotThrowOrFailAsync(details => {
                Logger.LogInformation("{@ProjectionDefinition}", details.Definition);
                details.Definition.ShouldBe<ProjectionDefinition>(AnotherTestDefinition);
            });
    }

    [Test, TimeoutAfter.SixtySeconds]
    public async Task updates_projection_settings(CancellationToken ct) {
        ProjectionName projection = NewEntityID();

        await AutomaticClient.Projections
            .Create(projection, TestDefinition, ProjectionSettings.Default, ct)
            .ShouldNotThrowOrFailAsync();

        await AutomaticClient.Projections
            .Disable(projection, ct)
            .ShouldNotThrowOrFailAsync();

        var newSettings = ProjectionSettings.Default with {
            CheckpointHandledThreshold = 99,
            MaxAllowedWritesInFlight   = 1000,
            CheckpointAfter            = 60000
        };

        await AutomaticClient.Projections
            .UpdateSettings(projection, newSettings, ct)
            .ShouldNotThrowOrFailAsync();

        await AutomaticClient.Projections
            .GetSettings(projection, ct)
            .ShouldNotThrowOrFailAsync(settings => {
                Logger.LogInformation("{@ProjectionSettings}", settings);
                settings.ShouldBeEquivalentTo(newSettings);
            });
    }

    [Test, TimeoutAfter.SixtySeconds]
    public async Task gets_projection_settings(CancellationToken ct) {
        ProjectionName projection = NewEntityID();

        await AutomaticClient.Projections
            .Create(projection, TestDefinition, ProjectionSettings.Default, ct)
            .ShouldNotThrowOrFailAsync();

        await AutomaticClient.Projections
            .GetSettings(projection, ct)
            .ShouldNotThrowOrFailAsync(settings => {
                Logger.LogInformation("{@ProjectionSettings}", settings);
                settings.ShouldNotBeNull();
            });
    }

    [Test, TimeoutAfter.SixtySeconds]
    public async Task deletes_projection(CancellationToken ct) {
        ProjectionName projection = NewEntityID();

        await AutomaticClient.Projections
            .Create(projection, TestDefinition, ProjectionSettings.Default, ct)
            .ShouldNotThrowOrFailAsync();

        await AutomaticClient.Projections
            .Delete(projection, DeleteProjectionOptions.Default, ct)
            .ShouldNotThrowOrFailAsync();

        await AutomaticClient.Projections
            .GetDetails(projection, ct)
            .ShouldNotThrowAsync()
            .OnSuccessAsync(details => Logger.LogWarning("Projection {ProjectionName} still exists after deletion: {@ProjectionDetails}", projection, details))
            .ShouldFailAsync(error =>
                error.Case.ShouldBe(GetProjectionDetailsError.GetProjectionDetailsErrorCase.NotFound));

        // if (result.IsSuccess) {
        //     Logger.LogWarning("Projection {ProjectionName} still exists after deletion: {@ProjectionStatus}", projection, result.Value.Status);
        //     return;
        // }
        //
        // result.IsFailure.ShouldBeTrue($"Projection should not exist after deletion");
        // result.Error.Case.ShouldBe(GetProjectionDetailsError.GetProjectionDetailsErrorCase.NotFound);
    }

    [Test, TimeoutAfter.SixtySeconds]
    public async Task enables_projection(CancellationToken ct) {
        ProjectionName projection = NewEntityID();

        await AutomaticClient.Projections
            .Create(projection, TestDefinition, ProjectionSettings.Default, ct)
            .ShouldNotThrowOrFailAsync();

        await AutomaticClient.Projections
            .Enable(projection, ct)
            .ShouldNotThrowOrFailAsync();

        await AutomaticClient.Projections
            .GetDetails(projection, ct)
            .ShouldNotThrowOrFailAsync(details => {
                Logger.LogInformation("{@ProjectionDetails}", details);
                details.Status.ShouldBe(ProjectionStatus.Running);
                details.ShouldNotBeNull();
            });
    }

    [Test, TimeoutAfter.SixtySeconds]
    public async Task enables_already_enabled_projection(CancellationToken ct) {
        ProjectionName projection = NewEntityID();

        await AutomaticClient.Projections
            .Create(projection, TestDefinition, ProjectionSettings.Default, ct)
            .ShouldNotThrowOrFailAsync();

        await AutomaticClient.Projections
            .Enable(projection, ct)
            .ShouldNotThrowOrFailAsync();

        await AutomaticClient.Projections
            .Enable(projection, ct)
            .ShouldNotThrowOrFailAsync();

        await AutomaticClient.Projections
            .GetDetails(projection, ct)
            .ShouldNotThrowOrFailAsync(details => {
                Logger.LogInformation("{@ProjectionDetails}", details);
                details.Status.ShouldBe(ProjectionStatus.Running);
            });
    }

    [Test, TimeoutAfter.SixtySeconds]
    public async Task disables_projection(CancellationToken ct) {
        ProjectionName projection = NewEntityID();

        await AutomaticClient.Projections
            .Create(projection, TestDefinition, ProjectionSettings.Default, ct)
            .ShouldNotThrowOrFailAsync();

        await AutomaticClient.Projections
            .Enable(projection, ct)
            .ShouldNotThrowOrFailAsync();

        await AutomaticClient.Projections
            .Disable(projection, ct)
            .ShouldNotThrowOrFailAsync();

        var details = await AutomaticClient.Projections
            .GetDetails(projection, ct)
            .ShouldNotThrowOrFailAsync();

        Logger.LogInformation("{@ProjectionDetails}", details);

        details.ShouldNotBeNull();
    }

    [Test, TimeoutAfter.SixtySeconds]
    public async Task disables_already_disabled_projection(CancellationToken ct) {
        ProjectionName projection = NewEntityID();

        await AutomaticClient.Projections
            .Create(projection, TestDefinition, ProjectionSettings.Default, ct)
            .ShouldNotThrowOrFailAsync();

        await AutomaticClient.Projections
            .Enable(projection, ct)
            .ShouldNotThrowOrFailAsync();

        await AutomaticClient.Projections
            .Disable(projection, ct)
            .ShouldNotThrowOrFailAsync();

        await AutomaticClient.Projections
            .Disable(projection, ct)
            .ShouldNotThrowOrFailAsync();

        await AutomaticClient.Projections
            .GetDetails(projection, ct)
            .ShouldNotThrowOrFailAsync(details => {
                Logger.LogInformation("{@ProjectionDetails}", details);
                details.Status.ShouldBe(ProjectionStatus.Stopped);
            });
    }

    [Test, TimeoutAfter.SixtySeconds]
    public async Task resets_projection(CancellationToken ct) {
        ProjectionName projection = NewEntityID();

        await AutomaticClient.Projections
            .Create(projection, TestDefinition, ProjectionSettings.Default, ct)
            .ShouldNotThrowOrFailAsync();

        await AutomaticClient.Projections
            .Enable(projection, ct)
            .ShouldNotThrowOrFailAsync();

        await AutomaticClient.Projections
            .Reset(projection, ct)
            .ShouldNotThrowOrFailAsync();

        await AutomaticClient.Projections
            .GetDetails(projection, ct)
            .ShouldNotThrowOrFailAsync(details => {
                Logger.LogInformation("{@ProjectionDetails}", details);
                details.ShouldNotBeNull();
            });
    }

    [Test, TimeoutAfter.SixtySeconds]
    public async Task gets_projection(CancellationToken ct) {
        ProjectionName projection = NewEntityID();

        await AutomaticClient.Projections
            .Create(projection, TestDefinition, ProjectionSettings.Default, ct)
            .ShouldNotThrowOrFailAsync();

        await AutomaticClient.Projections
            .GetDetails(projection, ct)
            .ShouldNotThrowOrFailAsync(details => {
                Logger.LogInformation("{@ProjectionDetails}", details);
                details.Name.ShouldBe(projection);
                details.Definition.ShouldBe(ProjectionDefinition.From(TestDefinition));
                details.Settings.ShouldBe(ProjectionSettings.Default);
                details.Status.ShouldBe(ProjectionStatus.Stopped);
            });
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

    [Test, TimeoutAfter.SixtySeconds, SystemProjectionsCases]
    public async Task gets_system_projections(ProjectionName projection, CancellationToken ct) {
        await AutomaticClient.Projections
            .GetDetails(projection, ct)
            .ShouldNotThrowOrFailAsync(details => {
                Logger.LogInformation("{@ProjectionDetails}", details);
                details.Name.IsSystemProjection.ShouldBeTrue();
                details.Name.ShouldBe(projection);
                details.Definition.ShouldBe(ProjectionDefinition.None);
                details.Settings.ShouldBe(ProjectionSettings.Default);
                details.Status.ShouldBe(ProjectionStatus.Running);
            });
    }

    [Test, TimeoutAfter.SixtySeconds]
    public async Task lists_projections(CancellationToken ct) {
        await AutomaticClient.Projections
            .Create(NewEntityID(), TestDefinition, ProjectionSettings.Default, ct)
            .ShouldNotThrowOrFailAsync();

        await AutomaticClient.Projections
            .List(ListProjectionsOptions.Default, ct)
            .ShouldNotThrowOrFailAsync(details => {
                Logger.LogInformation("{@Projections}", details);
                details.Count.ShouldBePositive();
            });
    }

    [Test, TimeoutAfter.SixtySeconds]
    public async Task lists_system_projections(CancellationToken ct) {
        var options = new ListProjectionsOptions {
            Type              = ProjectionType.System,
            IncludeDefinition = true, // it's ignored for system projections
            IncludeSettings   = true,
            IncludeStatistics = true
        };

        await AutomaticClient.Projections
            .List(options, ct)
            .ShouldNotThrowOrFailAsync(result => {
                Logger.LogInformation("{@Projections}", result);
                result.Count.ShouldBePositive();
                result.Any(x => x.HasDefinition).ShouldBeFalse();
                result.Any(x => x.HasSettings).ShouldBeTrue();
                result.Any(x => x.HasStatistics).ShouldBeTrue();
            });
    }

    [Test, TimeoutAfter.SixtySeconds]
    public async Task lists_projections_excluding_definition(CancellationToken ct) {
        await AutomaticClient.Projections
            .Create(NewEntityID(), TestDefinition, ProjectionSettings.Default, ct)
            .ShouldNotThrowOrFailAsync();

        var options = new ListProjectionsOptions {
            Type              = ProjectionType.User,
            IncludeDefinition = false,
            IncludeSettings   = true,
            IncludeStatistics = true
        };

        await AutomaticClient.Projections
            .List(options, ct)
            .ShouldNotThrowOrFailAsync(result => {
                Logger.LogInformation("{@Projections}", result);
                result.Count.ShouldBePositive();
                result.Any(x => x.HasDefinition).ShouldBeFalse();
                result.Any(x => x.HasSettings).ShouldBeTrue();
                result.Any(x => x.HasStatistics).ShouldBeTrue();
            });
    }

    [Test, TimeoutAfter.SixtySeconds]
    public async Task lists_projections_excluding_settings(CancellationToken ct) {
        await AutomaticClient.Projections
            .Create(NewEntityID(), TestDefinition, ProjectionSettings.Default, ct)
            .ShouldNotThrowOrFailAsync();

        var options = new ListProjectionsOptions {
            Type              = ProjectionType.User,
            IncludeDefinition = true,
            IncludeSettings   = false,
            IncludeStatistics = true
        };

        await AutomaticClient.Projections
            .List(options, ct)
            .ShouldNotThrowOrFailAsync(result => {
                Logger.LogInformation("{@Projections}", result);
                result.Count.ShouldBePositive();
                result.Any(x => x.HasDefinition).ShouldBeTrue();
                result.Any(x => x.HasSettings).ShouldBeFalse();
                result.Any(x => x.HasStatistics).ShouldBeTrue();
            });
    }

    [Test, TimeoutAfter.SixtySeconds]
    public async Task lists_projections_excluding_stats(CancellationToken ct) {
        await AutomaticClient.Projections
            .Create(NewEntityID(), TestDefinition, ProjectionSettings.Default, ct)
            .ShouldNotThrowOrFailAsync();

        var options = new ListProjectionsOptions {
            Type              = ProjectionType.User,
            IncludeDefinition = true,
            IncludeSettings   = true,
            IncludeStatistics = false
        };

        await AutomaticClient.Projections
            .List(options, ct)
            .ShouldNotThrowOrFailAsync(result => {
                Logger.LogInformation("{@Projections}", result);
                result.Count.ShouldBePositive();
                result.Any(x => x.HasDefinition).ShouldBeTrue();
                result.Any(x => x.HasSettings).ShouldBeTrue();
                result.Any(x => x.HasStatistics).ShouldBeFalse();
            });
    }

    [Test, TimeoutAfter.SixtySeconds]
    public async Task fails_to_create_projection_with_already_existing_name(CancellationToken ct) {
        ProjectionName projection = NewEntityID();

        await AutomaticClient.Projections
            .Create(projection, TestDefinition, ProjectionSettings.Default, ct)
            .ShouldNotThrowOrFailAsync();

        await AutomaticClient.Projections
            .Create(projection, TestDefinition, ProjectionSettings.Default, ct)
            .ShouldFailAsync(error =>
                error.Case.ShouldBe(CreateProjectionError.CreateProjectionErrorCase.AlreadyExists));
    }

    [Test, TimeoutAfter.SixtySeconds]
    public async Task fails_to_enable_non_existing_projection(CancellationToken ct) {
        await AutomaticClient.Projections
            .Enable(NewEntityID(), ct)
            .ShouldFailAsync(error =>
                error.Case.ShouldBe(EnableProjectionError.EnableProjectionErrorCase.NotFound));
    }

    [Test, TimeoutAfter.FiveSeconds]
    public async Task fails_to_disable_non_existing_projection(CancellationToken ct) {
        await AutomaticClient.Projections
            .Disable(NewEntityID(), ct)
            .ShouldFailAsync(error =>
                error.Case.ShouldBe(DisableProjectionError.DisableProjectionErrorCase.NotFound));
    }

    [Test, TimeoutAfter.SixtySeconds]
    public async Task fails_to_delete_non_existing_projection(CancellationToken ct) {
        ProjectionName projection = NewEntityID();

        await AutomaticClient.Projections
            .Delete(projection, DeleteProjectionOptions.Default, ct)
            .ShouldFailAsync(error =>
                error.Case.ShouldBe(DeleteProjectionError.DeleteProjectionErrorCase.NotFound));
    }

    [Test, TimeoutAfter.SixtySeconds]
    public async Task fails_to_delete_running_projection(CancellationToken ct) {
        ProjectionName projection = NewEntityID();

        await AutomaticClient.Projections
            .Create(projection, TestDefinition, ProjectionSettings.Default, ct)
            .ShouldNotThrowOrFailAsync();

        await AutomaticClient.Projections
            .Enable(projection, ct)
            .ShouldNotThrowOrFailAsync();

        await AutomaticClient.Projections
            .Delete(projection, DeleteProjectionOptions.Default, ct)
            .ShouldFailAsync(error =>
                error.Case.ShouldBe(DeleteProjectionError.DeleteProjectionErrorCase.FailedPrecondition));
    }

    [Test, TimeoutAfter.SixtySeconds]
    public async Task fails_to_reset_non_existing_projection(CancellationToken ct) {
        await AutomaticClient.Projections
            .Reset(NewEntityID(), ct)
            .ShouldFailAsync(error =>
                error.Case.ShouldBe(ResetProjectionError.ResetProjectionErrorCase.NotFound));
    }

    [Test, TimeoutAfter.SixtySeconds]
    public async Task fails_to_get_non_existing_projection(CancellationToken ct) {
        await AutomaticClient.Projections
            .GetDetails(NewEntityID(), ct)
            .ShouldFailAsync(error =>
                error.Case.ShouldBe(GetProjectionDetailsError.GetProjectionDetailsErrorCase.NotFound));
    }
}
