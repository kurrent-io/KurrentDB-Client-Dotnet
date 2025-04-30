using Ductus.FluentDocker.Builders;
using Ductus.FluentDocker.Services;

namespace KurrentDB.Client.Tests.FluentDocker;

public abstract class TestCompositeService : TestService<ICompositeService, CompositeBuilder>;
