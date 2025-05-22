// using KurrentDB.Client.Model;
//
// namespace KurrentDB.Client.SchemaRegistry.Serialization;
//
// #pragma warning disable CS1998
// /// <summary>
// /// Represents configuration policies for schema registration and validation.
// /// </summary>
// public record SchemaRegistrationPolicies(bool AutoRegister, bool Validate, SchemaDataFormat EnforcedDataFormat, bool AutoMap) {
// 	public static readonly SchemaRegistrationPolicies None = new(false, false, SchemaDataFormat.Unspecified, false);
//
// 	public bool SchemaRegistryRequired => AutoRegister || Validate;
//
// 	/// <summary>
// 	/// Ensures that the specified schema data format is allowed based on the current policies.
// 	/// Throws an exception if the data format is not allowed.
// 	/// </summary>
// 	public SchemaRegistrationPolicies EnsureDataFormatAllowed(SchemaDataFormat dataFormat) {
// 		if (EnforcedDataFormat == SchemaDataFormat.Unspecified || EnforcedDataFormat == dataFormat)
// 			return this;
//
// 		throw new NonCompliantSchemaDataFormatException(dataFormat, EnforcedDataFormat);
// 	}
//
// 	public static SchemaRegistrationPolicies From(SchemaRegistryServerConfiguration serverConfig, SchemaSerializerOptions options) {
// 		if (serverConfig.SchemaRegistryEnabled) {
// 			// Determine auto-registration based on server policy and user options
// 			var autoRegister = serverConfig.AutoRegistration.Enforced
// 				? serverConfig.AutoRegistration.Required // Server has a policy, follow it regardless of user preference
// 				: options.AutoRegister;                  // Server has no policy, use user preference
//
// 			// Validate if either server enforces it or user has requested it
// 			// Additionally, if auto-registration is enforced as disabled, validation must be enabled
// 			var validate = serverConfig.Validation.Enforced
// 			            || options.Validate
// 			            || serverConfig.AutoRegistration is { Enforced: true, Required: false };
//
// 			return new(
// 				AutoRegister: autoRegister,
// 				Validate: validate,
// 				EnforcedDataFormat: serverConfig.SchemaDataFormat.EnforcedFormat,
// 				// Always map locally if user requested auto-registration
// 				AutoMap: options.AutoRegister
// 			);
// 		}
// 		else {
// 			// When schema registry is disabled, we can only do local operations
// 			return new(
// 				AutoRegister: false,
// 				Validate: false,
// 				EnforcedDataFormat: SchemaDataFormat.Unspecified,
// 				AutoMap: options.AutoRegister
// 			);
// 		}
// 	}
// }
