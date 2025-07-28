using System.Diagnostics.CodeAnalysis;
using Ductus.FluentDocker.Builders;
using Ductus.FluentDocker.Common;
using Ductus.FluentDocker.Model.Containers;
using Ductus.FluentDocker.Services;
using JetBrains.Annotations;
using Serilog;
using static System.Environment;
using static System.String;
using static System.StringComparison;

namespace Kurrent.Client.Testing.Containers.FluentDocker;

[PublicAPI]
public abstract class TestContainer {
    protected static readonly ILogger Logger = Log.ForContext<TestContainer>();

    static TestContainer() => Ductus.FluentDocker.Services.Logging.Enabled();

    protected TestContainer(string defaultImage, string? serviceName = null) {
        ArgumentException.ThrowIfNullOrWhiteSpace(defaultImage);

        ServiceName = GetTestContainerServiceName(serviceName ??  GetType().Name);
        Image       = GetContainerImageNameOrDefault(ServiceName, defaultImage);
    }

    protected string ServiceName { get; }
    protected string Image       { get; }

    // protected string Enabled     { get; }

    public IContainerService Service { get; private set; } = null!;

    public Container Configuration => Service.GetConfiguration(true);

    protected abstract ContainerBuilder ConfigureContainer(ContainerBuilder builder);

    public async ValueTask Start() {
        try {
            Service = ConfigureContainer(
                new Builder()
                    .UseContainer()
                    .WithName(ServiceName)
                    .UseImage(Image)
                    .WithPublicEndpointResolver()
            ).Build();

            // foreach (var serviceState in Enum.GetValues<ServiceRunningState>()) {
            //     Service.AddHook(
            //         state: serviceState,
            //         hook: service => Logger.Verbose("{ServiceName} State:{ServiceState}", service.Name, serviceState),
            //         uniqueName: $"{Service.Name}-{serviceState}".ToLowerInvariant()
            //     );
            // }

            await Task.Run(() => Service.Start()).ConfigureAwait(false);

            Logger.Information(
                "{ServiceName} Container started: {@ExposedPorts}",
                ServiceName, Service.GetConfiguration().Config.ExposedPorts.Keys
            );
        }
        catch (Exception ex) {
            Logger.Error(ex, "{ServiceName} Failed to start container", ServiceName);
            throw new FluentDockerException($"{ServiceName} Failed to start container", ex);
        }

        try {
            await OnStarted().ConfigureAwait(false);
        }
        catch (Exception ex) {
            throw new FluentDockerException($"{ServiceName} {nameof(OnStarted)} Execution error", ex);
        }
    }

    public async ValueTask Stop() {
        try {
            Service.Stop();
            Logger.Information("{ServiceName} Container stopped", ServiceName);
        }
        catch (Exception ex) {
            throw new FluentDockerException($"{ServiceName} Failed to stop container", ex);
        }

        try {
            await OnStopped().ConfigureAwait(false);
        }
        catch (Exception ex) {
            throw new FluentDockerException($"{ServiceName} {nameof(OnStopped)} Execution error", ex);
        }
    }

    public void ReportStatus() {
        var cfg = Service.GetConfiguration(true);

        Logger.Debug(
            "Container {ContainerName} Created On: {CreatedOn} Exposed Ports: {@ExposedPorts}",
            cfg.Name, cfg.Created, cfg.Config.ExposedPorts.Keys
        );
    }

    public virtual ValueTask DisposeAsync() {
        try {
            Service.Dispose();
        }
        catch (Exception ex) {
            throw new FluentDockerException("Failed to gracefully dispose of container service", ex);
        }

        return ValueTask.CompletedTask;
    }

    protected virtual ValueTask OnStarted() => ValueTask.CompletedTask;
    protected virtual ValueTask OnStopped() => ValueTask.CompletedTask;

    #region . helpers .

    public static string GetTestContainerServiceName(string serviceName) =>
        serviceName.Replace("TestContainer", Empty, OrdinalIgnoreCase).Trim().ToLowerInvariant();

    public static string GenerateTestContainerEnvVar(string serviceName) {
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);

        var serviceTag = GetTestContainerServiceName(serviceName)
            .Replace("-", "").Replace("_", "");

        return $"TESTCONTAINER_{serviceTag}_IMAGE";
    }

    public static string GetTestContainerEnabledEnvVar(string serviceName) {
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);

        var serviceTag = GetTestContainerServiceName(serviceName)
            .Replace("-", "").Replace("_", "");

        return $"TESTCONTAINER_{serviceTag}_ENABLED";
    }

    public static bool TryGetContainerImageName(string serviceName, [MaybeNullWhen(false)] out string imageName) =>
        !IsNullOrWhiteSpace(imageName = GetEnvironmentVariable(GenerateTestContainerEnvVar(serviceName)));

    public static bool TryGetContainerImageName(Type containerType, [MaybeNullWhen(false)] out string imageName) =>
        TryGetContainerImageName(containerType.Name, out imageName);

    public static bool TryGetContainerImageName<T>([MaybeNullWhen(false)] out string imageName) where T : TestContainer =>
        TryGetContainerImageName(typeof(T).Name, out imageName);

    public static string GetContainerImageNameOrDefault(string serviceName, string defaultImage) {
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);
        ArgumentException.ThrowIfNullOrWhiteSpace(defaultImage);
        return TryGetContainerImageName(serviceName, out var imageName) ? imageName : defaultImage;
    }

    #endregion
}
