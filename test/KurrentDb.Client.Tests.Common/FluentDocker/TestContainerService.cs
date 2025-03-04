using Ductus.FluentDocker.Builders;
using Ductus.FluentDocker.Services;

namespace KurrentDb.Client.Tests.FluentDocker;

public abstract class TestContainerService : TestService<IContainerService, ContainerBuilder>;
