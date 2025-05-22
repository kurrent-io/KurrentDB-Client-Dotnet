// using KurrentDB.Client.Model;
//
// namespace KurrentDB.Client.SchemaRegistry;
//
// /// <summary>
// /// Exception thrown when a schema data format does not comply with the format required by the schema registry governance policy.
// /// </summary>
// public class NonCompliantSchemaDataFormatException : Exception {
// 	/// <summary>
// 	/// Initializes a new instance of the <see cref="NonCompliantSchemaDataFormatException"/> class.
// 	/// </summary>
// 	/// <param name="providedFormat">The data format that was provided but does not comply with policy.</param>
// 	/// <param name="requiredFormat">The data format that is required according to the schema registry governance policy.</param>
// 	public NonCompliantSchemaDataFormatException(SchemaDataFormat providedFormat, SchemaDataFormat requiredFormat)
// 		: base(FormatMessage(providedFormat, requiredFormat)) {
// 		ProvidedFormat = providedFormat;
// 		RequiredFormat = requiredFormat;
// 	}
//
// 	/// <summary>
// 	/// Gets the data format that was provided but does not comply with policy.
// 	/// </summary>
// 	public SchemaDataFormat ProvidedFormat { get; }
//
// 	/// <summary>
// 	/// Gets the data format that is required according to the schema registry governance policy.
// 	/// </summary>
// 	public SchemaDataFormat RequiredFormat { get; }
//
// 	static string FormatMessage(SchemaDataFormat providedFormat, SchemaDataFormat requiredFormat) =>
// 		$"Schema registry is enabled and data format enforcement is active. " +
// 		$"The data format '{providedFormat}' is not allowed. It must be '{requiredFormat}'.";
// }
//
// /// <summary>
// /// Defines the policy requirement level for a server feature
// /// </summary>
// public enum PolicyRequirement {
// 	/// <summary>
// 	/// The feature is optional - client controls the behavior
// 	/// </summary>
// 	Optional,
//
// 	/// <summary>
// 	/// The feature is required - must be enabled
// 	/// </summary>
// 	Required,
//
// 	/// <summary>
// 	/// The feature is prohibited - must be disabled
// 	/// </summary>
// 	Prohibited
// }
//
// /// <summary>
// /// Server-side schema registry configuration
// /// </summary>
// public class SchemaRegistryServerConfiguration {
// 	/// <summary>
// 	/// Whether the schema registry is enabled on the server
// 	/// </summary>
// 	public bool Enabled { get; set; } = true;
//
// 	/// <summary>
// 	/// Server policy for schema registration
// 	/// </summary>
// 	public ServerRegistrationPolicy RegistrationPolicy { get; set; } = new();
//
// 	/// <summary>
// 	/// Server policy for schema validation
// 	/// </summary>
// 	public ServerValidationPolicy ValidationPolicy { get; set; } = new();
//
// 	/// <summary>
// 	/// Server policy for schema data format
// 	/// </summary>
// 	public ServerFormatPolicy FormatPolicy { get; set; } = new();
// }
//
// /// <summary>
// /// Server policy for schema registration
// /// </summary>
// public class ServerRegistrationPolicy {
// 	/// <summary>
// 	/// The registration requirement enforced by the server
// 	/// </summary>
// 	public PolicyRequirement Requirement { get; set; } = PolicyRequirement.Optional;
// }
//
// /// <summary>
// /// Server policy for schema validation
// /// </summary>
// public class ServerValidationPolicy {
// 	/// <summary>
// 	/// The validation requirement enforced by the server
// 	/// </summary>
// 	public PolicyRequirement Requirement { get; set; } = PolicyRequirement.Optional;
// }
//
// /// <summary>
// /// Server policy for schema data format
// /// </summary>
// public class ServerFormatPolicy {
// 	/// <summary>
// 	/// Whether a specific format is enforced by the server
// 	/// </summary>
// 	public bool IsEnforced => RequiredFormat != SchemaDataFormat.Unspecified;
//
// 	/// <summary>
// 	/// The format required by the server, or Unspecified if not enforced
// 	/// </summary>
// 	public SchemaDataFormat RequiredFormat { get; set; } = SchemaDataFormat.Unspecified;
// }
//
// //
// // /// <summary>
// // /// Defines the policy requirement level for a server feature
// // /// </summary>
// // public enum PolicyRequirement {
// // 	/// <summary>
// // 	/// The feature is optional - client controls the behavior
// // 	/// </summary>
// // 	Optional,
// //
// // 	/// <summary>
// // 	/// The feature is required - must be enabled
// // 	/// </summary>
// // 	Required,
// //
// // 	/// <summary>
// // 	/// The feature is prohibited - must be disabled
// // 	/// </summary>
// // 	Prohibited
// // }
// //
// //
// // /// <summary>
// // /// Server policy for schema registration
// // /// </summary>
// // public class ServerRegistrationPolicy
// // {
// // 	/// <summary>
// // 	/// How registration is enforced by the server
// // 	/// </summary>
// // 	public ServerPolicyEnforcement Enforcement { get; set; } = ServerPolicyEnforcement.NotEnforced;
// // }
// //
// // /// <summary>
// // /// Server policy for schema validation
// // /// </summary>
// // public class ServerValidationPolicy
// // {
// // 	/// <summary>
// // 	/// How validation is enforced by the server
// // 	/// </summary>
// // 	public ServerPolicyEnforcement Enforcement { get; set; } = ServerPolicyEnforcement.NotEnforced;
// // }
// //
// // /// <summary>
// // /// Server policy for schema data format
// // /// </summary>
// // public class ServerFormatPolicy
// // {
// // 	/// <summary>
// // 	/// Whether a specific format is enforced by the server
// // 	/// </summary>
// // 	public bool IsEnforced => RequiredFormat != SchemaDataFormat.Unspecified;
// //
// // 	/// <summary>
// // 	/// The format required by the server, or Unspecified if not enforced
// // 	/// </summary>
// // 	public SchemaDataFormat RequiredFormat { get; set; } = SchemaDataFormat.Unspecified;
// // }
// //
// //
// // /// <summary>
// // /// Server-side schema registry configuration
// // /// </summary>
// // public class SchemaRegistryServerConfiguration {
// // 	/// <summary>
// // 	/// Whether the schema registry is enabled on the server
// // 	/// </summary>
// // 	public bool Enabled { get; set; } = true;
// //
// // 	/// <summary>
// // 	/// Server policy for schema registration
// // 	/// </summary>
// // 	public ServerRegistrationPolicy RegistrationPolicy { get; set; } = new();
// //
// // 	/// <summary>
// // 	/// Server policy for schema validation
// // 	/// </summary>
// // 	public ServerValidationPolicy ValidationPolicy { get; set; } = new();
// //
// // 	/// <summary>
// // 	/// Server policy for schema data format
// // 	/// </summary>
// // 	public ServerFormatPolicy FormatPolicy { get; set; } = new();
// // }
// //
// // /// <summary>
// // /// Server policy for schema registration
// // /// </summary>
// // public class ServerRegistrationPolicy {
// // 	/// <summary>
// // 	/// Whether the server policy is enforced
// // 	/// </summary>
// // 	public bool Enforced { get; set; }
// //
// // 	/// <summary>
// // 	/// If enforced, whether registration is required or forbidden
// // 	/// </summary>
// // 	public bool Required { get; set; }
// // }
// //
// // /// <summary>
// // /// Server policy for schema validation
// // /// </summary>
// // public class ServerValidationPolicy {
// // 	/// <summary>
// // 	/// Whether the server policy is enforced
// // 	/// </summary>
// // 	public bool Enforced { get; set; }
// //
// // 	/// <summary>
// // 	/// If enforced, whether validation is required or forbidden
// // 	/// </summary>
// // 	public bool Required { get; set; }
// // }
// //
// // /// <summary>
// // /// Server policy for schema data format
// // /// </summary>
// // public class ServerFormatPolicy {
// // 	/// <summary>
// // 	/// Whether the server policy is enforced
// // 	/// </summary>
// // 	public bool Enforced { get; set; }
// //
// // 	/// <summary>
// // 	/// If enforced, which format is required
// // 	/// </summary>
// // 	public SchemaDataFormat RequiredFormat { get; set; } = SchemaDataFormat.Unspecified;
// // }
// //
// // //
// // // /// <summary>
// // // /// Represents the server-side configuration for the schema registry that connected clients must follow.
// // // /// </summary>
// // // public record SchemaRegistryServerConfiguration {
// // // 	/// <summary>
// // // 	/// Settings that control automatic registration.
// // // 	/// </summary>
// // // 	public AutoRegistrationSettings AutoRegistration { get; init; } = new();
// // //
// // // 	/// <summary>
// // // 	/// Settings that control schema validation.
// // // 	/// </summary>
// // // 	public ValidationSettings Validation { get; init; } = new();
// // //
// // // 	/// <summary>
// // // 	/// Settings that control compatibility enforcement.
// // // 	/// </summary>
// // // 	public CompatibilitySettings Compatibility { get; init; } = new();
// // //
// // // 	/// <summary>
// // // 	/// Settings that control schema data format enforcement.
// // // 	/// </summary>
// // // 	public SchemaDataFormatSettings SchemaDataFormat { get; init; } = new();
// // //
// // // 	/// <summary>
// // // 	/// Indicates whether the schema registry feature is available on the server.
// // // 	/// based on the enabled status of sub-settings including automatic registration,
// // // 	/// validation, compatibility enforcement, or data format enforcement.
// // // 	/// </summary>
// // // 	public bool SchemaRegistryEnabled { get; init; } = false;
// // // }
// // //
// // // /// <summary>
// // // /// Configures automatic schema registration settings enforced by the server.
// // // /// </summary>
// // // public record AutoRegistrationSettings {
// // // 	/// <summary>
// // // 	/// Indicates whether the server has a policy about auto-registration.
// // // 	/// </summary>
// // // 	/// <value>
// // // 	/// <c>true</c> if server enforces a specific policy; <c>false</c> if server lets clients decide.
// // // 	/// </value>
// // // 	public bool Enforced { get; init; } = false;
// // //
// // // 	/// <summary>
// // // 	/// When policy is enforced, indicates whether auto-registration should be enabled or disabled.
// // // 	/// Only relevant when <see cref="Enforced"/> is <c>true</c>.
// // // 	/// </summary>
// // // 	/// <value>
// // // 	/// <c>true</c> to force auto-registration; <c>false</c> to prohibit auto-registration.
// // // 	/// </value>
// // // 	public bool Required { get; init; } = false;
// // // }
// // //
// // // /// <summary>
// // // /// Configures schema validation settings enforced by the server.
// // // /// </summary>
// // // /// <remarks>
// // // /// When enabled, the server will perform schema validation on data during operations.
// // // /// </remarks>
// // // public record ValidationSettings {
// // // 	/// <summary>
// // // 	/// Indicates whether schema validation is enabled on the server.
// // // 	/// </summary>
// // // 	/// <value>
// // // 	/// <c>true</c> to enable schema validation; otherwise, <c>false</c>.
// // // 	/// Default is <c>true</c>.
// // // 	/// </value>
// // // 	public bool Enforced { get; init; } = true;
// // // }
// // //
// // // /// <summary>
// // // /// Configures schema compatibility settings enforced by the server.
// // // /// </summary>
// // // /// <remarks>
// // // /// Controls how the server checks new schema versions for compatibility with existing versions.
// // // /// </remarks>
// // // public record CompatibilitySettings {
// // // 	/// <summary>
// // // 	/// Indicates whether compatibility rules are enforced by the server.
// // // 	/// </summary>
// // // 	/// <value>
// // // 	/// <c>true</c> to enforce compatibility rules; otherwise, <c>false</c>.
// // // 	/// Default is <c>false</c>.
// // // 	/// </value>
// // // 	public bool Enforced { get; init; } = false;
// // //
// // // 	/// <summary>
// // // 	/// The compatibility mode that determines how schemas are checked for compatibility by the server.
// // // 	/// </summary>
// // // 	/// <value>
// // // 	/// The compatibility mode to use when checking schema compatibility.
// // // 	/// Default is <see cref="CompatibilityMode.Backward"/>.
// // // 	/// </value>
// // // 	public CompatibilityMode Mode { get; init; } = CompatibilityMode.Backward;
// // // }
// // //
// // // /// <summary>
// // // /// Configures schema data format settings enforced by the server.
// // // /// </summary>
// // // /// <remarks>
// // // /// Controls the enforcement and type of data format used for schemas on the server.
// // // /// </remarks>
// // // public record SchemaDataFormatSettings {
// // // 	/// <summary>
// // // 	/// The data format that the server enforces for schemas.
// // // 	/// When set to <see cref="SchemaDataFormat.Unspecified"/>, no format is enforced.
// // // 	/// </summary>
// // // 	public SchemaDataFormat EnforcedFormat { get; init; } = SchemaDataFormat.Unspecified;
// // //
// // // 	/// <summary>
// // // 	/// Indicates whether a specific data format is enforced.
// // // 	/// </summary>
// // // 	public bool IsEnforced => EnforcedFormat != SchemaDataFormat.Unspecified;
// // //
// // // }
