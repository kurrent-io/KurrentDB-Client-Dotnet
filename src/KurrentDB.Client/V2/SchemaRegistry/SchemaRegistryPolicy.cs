using Kurrent.Client.Model;

namespace Kurrent.Client.SchemaRegistry;

/// <summary>
/// Defines the schema registry policy for controlling schema registration, validation,
/// and formatting settings in the context of client and server operations.
/// </summary>
public record SchemaRegistryPolicy {
	public static readonly SchemaRegistryPolicy NoRequirements = new() {
		Enabled                 = true,
		RegistrationRequirement = PolicyRequirement.Optional,
		ValidationRequirement   = PolicyRequirement.Optional,
		EnforcedDataFormat      = SchemaDataFormat.Unspecified
	};

	public static readonly SchemaRegistryPolicy Disabled = new() { Enabled = false };

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
		var validate = options.ValidationMode switch {
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

		// If registration is prohibited by policy, ensure validation is enabled
		if (RegistrationRequirement == PolicyRequirement.Prohibited)
			validate = true;

		return new(autoRegister, validate, options.AutoMapMessages, EnforcedDataFormat, options.SchemaNameStrategy, stream);
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
