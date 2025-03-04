using Ductus.FluentDocker.Builders;
using Ductus.FluentDocker.Services;

namespace KurrentDB.Client.Tests.FluentDocker;

public abstract class TestContainerService : TestService<IContainerService, ContainerBuilder>;
