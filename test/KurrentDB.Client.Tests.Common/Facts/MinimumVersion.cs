// ReSharper disable InconsistentNaming

using KurrentDB.Client.Tests.FluentDocker;

namespace KurrentDB.Client.Tests;

[PublicAPI]
public class MinimumVersion {
	public class FactAttribute : Xunit.FactAttribute {
		readonly int  Major;
		readonly int? Minor;
		readonly int? Patch;

		public FactAttribute(int major) {
			Major = major;
		}

		public FactAttribute(int major, int minor) {
			Major = major;
			Minor = minor;
		}

		public FactAttribute(int major, int minor, int patch) {
			Major = major;
			Minor = minor;
			Patch = patch;
		}

		public override string? Skip {
			get {
				var currentVersion = TestContainerService.Version;
				var requiredVersionString = Patch.HasValue
					? $"{Major}.{Minor}.{Patch}"
					: Minor.HasValue
						? $"{Major}.{Minor}"
						: $"{Major}";

				if (Patch.HasValue) {
					var required = new Version(Major, Minor!.Value, Patch.Value);
					return currentVersion < required
						? $"Test requires minimum version {requiredVersionString}, but current version is {currentVersion}"
						: null;
				}

				if (Minor.HasValue) {
					if (currentVersion.Major < Major ||
					    (currentVersion.Major == Major && currentVersion.Minor < Minor.Value)) {
						return $"Test requires minimum version {requiredVersionString}, but current version is {currentVersion}";
					}
				} else {
					if (currentVersion.Major < Major)
						return $"Test requires minimum major version {requiredVersionString}, but current version is {currentVersion}";
				}

				return null;
			}
			set => throw new NotSupportedException();
		}
	}

	public class TheoryAttribute : Xunit.TheoryAttribute {
		readonly int  Major;
		readonly int? Minor;
		readonly int? Patch;

		public TheoryAttribute(int major) {
			Major = major;
		}

		public TheoryAttribute(int major, int minor) {
			Major = major;
			Minor = minor;
		}

		public TheoryAttribute(int major, int minor, int patch) {
			Major = major;
			Minor = minor;
			Patch = patch;
		}

		public override string? Skip {
			get {
				var currentVersion = TestContainerService.Version;
				var requiredVersionString = Patch.HasValue
					? $"{Major}.{Minor}.{Patch}"
					: Minor.HasValue
						? $"{Major}.{Minor}"
						: $"{Major}";

				if (Patch.HasValue) {
					var required = new Version(Major, Minor!.Value, Patch.Value);
					return currentVersion < required
						? $"Test requires minimum version {requiredVersionString}, but current version is {currentVersion}"
						: null;
				}

				if (Minor.HasValue) {
					if (currentVersion.Major < Major ||
					    (currentVersion.Major == Major && currentVersion.Minor < Minor.Value)) {
						return $"Test requires minimum version {requiredVersionString}, but current version is {currentVersion}";
					}
				} else {
					if (currentVersion.Major < Major)
						return $"Test requires minimum major version {requiredVersionString}, but current version is {currentVersion}";
				}

				return null;
			}
			set => throw new NotSupportedException();
		}
	}
}
