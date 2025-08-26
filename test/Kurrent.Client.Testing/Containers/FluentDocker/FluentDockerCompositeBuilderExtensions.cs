using System.Reflection;
using Ductus.FluentDocker.Builders;
using Ductus.FluentDocker.Model.Compose;
using JetBrains.Annotations;

namespace Kurrent.Client.Testing.Containers.FluentDocker;

[PublicAPI]
public static class FluentDockerCompositeBuilderExtensions {
    public static CompositeBuilder OverrideConfiguration(this CompositeBuilder compositeBuilder, Action<DockerComposeConfig> configure) {
        configure(GetInternalConfig(compositeBuilder));
        return compositeBuilder;

        static DockerComposeConfig GetInternalConfig(CompositeBuilder compositeBuilder) =>
            (DockerComposeConfig)typeof(CompositeBuilder)
                .GetField("_config", BindingFlags.NonPublic | BindingFlags.Instance)!
                .GetValue(compositeBuilder)!;
    }
}
