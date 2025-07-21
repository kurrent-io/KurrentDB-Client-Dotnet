using KurrentDB.Client.Tests.FluentDocker;

namespace KurrentDB.Client.Tests;

[PublicAPI]
public class Deprecation {
	public class FactAttribute(Version since, string skipMessage) : Xunit.FactAttribute {
		public override string? Skip {
			get => TestContainerService.Version >= since ? skipMessage : null;
			set => throw new NotSupportedException();
		}
	}

	public class TheoryAttribute(Version since, string skipMessage) : Xunit.TheoryAttribute {
		public override string? Skip {
			get => TestContainerService.Version >= since ? skipMessage : null;
			set => throw new NotSupportedException();
		}
	}
}
