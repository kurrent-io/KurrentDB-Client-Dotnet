using Kurrent.Client.Features;
using Kurrent.Client.Model;

namespace Kurrent.Client.SchemaRegistry;

/// <summary>
/// Defines the schema registry policy for controlling schema registration, validation,
/// and formatting settings in the context of client and server operations.
/// </summary>
public record SchemaRegistryPolicy {
	public static readonly SchemaRegistryPolicy Disabled = new() { Enabled = false };

	public static readonly SchemaRegistryPolicy NoRequirements = new() {
		Enabled                 = true,
		RegistrationRequirement = PolicyRequirement.Optional,
		ValidationRequirement   = PolicyRequirement.Optional,
		EnforcedDataFormat      = SchemaDataFormat.Unspecified
	};

	/// <summary>
	/// Whether the schema registry is enabled on the server
	/// </summary>
	public bool Enabled { get; init; } = true;

	/// <summary>
	/// The registration requirement enforced by the server
	/// </summary>
	public PolicyRequirement RegistrationRequirement { get; init; } = PolicyRequirement.Optional;

	/// <summary>
	/// The validation requirement enforced by the server
	/// </summary>
	public PolicyRequirement ValidationRequirement { get; init; } = PolicyRequirement.Optional;

	/// <summary>
	/// The format required by the server, or Unspecified if not enforced
	/// </summary>
	public SchemaDataFormat EnforcedDataFormat { get; init; } = SchemaDataFormat.Unspecified;

	/// <summary>
	/// Resolves the effective schema registry policy by considering both client-provided
	/// options and the server's schema registry policy settings.
	/// </summary>
	/// <param name="options">The schema registration options provided by the client.</param>
	/// <param name="stream">The stream identifier associated with the schema.</param>
	/// <returns>A <see cref="ResolvedSchemaRegistryPolicy"/> object that encapsulates the resolved settings.</returns>
	public ResolvedSchemaRegistryPolicy Resolve(SchemaRegistrationOptions options, string? stream) {
		// If schema registry is disabled on server, create a policy with local operations only
		if (!Enabled)
			return new(false, false, options.AutoMapMessages, SchemaDataFormat.Unspecified, options.SchemaNameStrategy, stream);

		// Resolve auto-registration based on client mode and server policy
		var autoRegister = options.RegistrationMode switch {
			SchemaRegistrationMode.Auto => RegistrationRequirement switch {
				PolicyRequirement.Required   => true,
				PolicyRequirement.Prohibited => false,
				_                            => true // Default to true for Auto mode
			},

			SchemaRegistrationMode.Manual => RegistrationRequirement switch {
				PolicyRequirement.Required   => true,
				PolicyRequirement.Prohibited => false,
				_                            => false // Default to false for Manual mode
			},

			SchemaRegistrationMode.ServerPolicy => RegistrationRequirement == PolicyRequirement.Required,

			_ => false
		};

		// Resolve validation based on client mode and server policy
		var validate = RegistrationRequirement is PolicyRequirement.Prohibited || options.ValidationMode switch {
			SchemaValidationMode.Enabled => ValidationRequirement switch {
				PolicyRequirement.Required   => true,
				PolicyRequirement.Prohibited => false,
				_                            => true // Default to true for Enabled mode
			},

			SchemaValidationMode.Disabled => ValidationRequirement switch {
				PolicyRequirement.Required   => true,
				PolicyRequirement.Prohibited => false,
				_                            => false // Default to false for Disabled mode
			},

			SchemaValidationMode.ServerPolicy => ValidationRequirement == PolicyRequirement.Required,

			_ => false
		};

		return new(autoRegister, validate, options.AutoMapMessages, EnforcedDataFormat, options.SchemaNameStrategy, stream);
	}

	/// <summary>
	/// Creates a schema registry policy from server capabilities.
	/// </summary>
	/// <param name="features">The server capabilities object containing schema registry information.</param>
	/// <returns>
	/// A configured <see cref="SchemaRegistryPolicy"/> based on server capabilities,
	/// or <see cref="SchemaRegistryPolicy.Disabled"/> if schema registry is not supported.
	/// </returns>
	public static SchemaRegistryPolicy FromServerFeatures(ServerFeatures features) {
		// Check if schema registry capability exists
		var capability = features.GetFeature("SchemaRegistry");

		// If capability is null or not enabled, return disabled policy
		if (capability is null && capability is not { Enabled: true })
			return Disabled;

		var registrationSupport = capability.GetRequirementValue("RegistrationSupport", false);
		var validationSupport   = capability.GetRequirementValue("ValidationSupport", false);

		// If neither registration nor validation is supported, consider it disabled
		if (!registrationSupport && !validationSupport)
			return Disabled;

		var registration   = capability.GetRequirementValue("Registration", PolicyRequirement.Optional);
		var validation     = capability.GetRequirementValue("Validation", PolicyRequirement.Optional);
		var enforcedFormat = capability.GetRequirementValue("DataFormat", SchemaDataFormat.Unspecified);

		return new SchemaRegistryPolicy {
			Enabled                 = true,
			RegistrationRequirement = registration,
			ValidationRequirement   = validation,
			EnforcedDataFormat      = enforcedFormat
		};
	}
}

/// <summary>
/// Defines the policy requirement level for a server feature
/// </summary>
public enum PolicyRequirement {
	/// <summary>
	/// The feature is optional - client controls the behavior
	/// </summary>
	Optional,

	/// <summary>
	/// The feature is required - must be enabled
	/// </summary>
	Required,

	/// <summary>
	/// The feature is prohibited - must be disabled
	/// </summary>
	Prohibited
}
