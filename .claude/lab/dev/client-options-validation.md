# KurrentClientOptions Validation Rules

This document defines the validation rules enforced by the `EnsureValid()` method of `KurrentClientOptions`. These rules help prevent runtime errors by validating the configuration before establishing a connection to KurrentDB.

## Table of Contents
- [Connection Configuration Rules](#connection-configuration-rules)
- [Endpoint Validation Rules](#endpoint-validation-rules)
- [Gossip Options Rules](#gossip-options-rules)
- [Resilience Options Rules](#resilience-options-rules)
- [Security Options Rules](#security-options-rules)
- [Schema Options Rules](#schema-options-rules)
- [General Rules](#general-rules)

## Connection Configuration Rules

### Direct Connection Endpoint Rule
- If `ConnectionScheme` is `Direct`, exactly one endpoint must be provided
- **Error**: "Direct connection must have exactly one endpoint."

### Discovery Connection Endpoints Rule
- If `ConnectionScheme` is `Discover`, at least one endpoint must be provided
- **Error**: "Discovery connection must have at least one endpoint."
- **Warning**: If more than 100 endpoints are provided, a warning is logged (but no error thrown)

## Endpoint Validation Rules

### Host Validation
- Each endpoint's `Host` must not be null or empty
- Each endpoint's `Host` must be a valid hostname or IP address
- **Error**: "Invalid hostname in endpoint: {host}"

### Port Validation
- Each endpoint's `Port` must be between 1 and 65535
- **Error**: "Invalid port number in endpoint: {port}. Port must be between 1 and 65535."

## Gossip Options Rules

### MaxDiscoverAttempts Validation
- `MaxDiscoverAttempts` must be greater than 0, unless set to -1 for infinite retries
- **Error**: "MaxDiscoverAttempts must be greater than 0 or -1 for infinite retries."

### DiscoveryInterval Validation
- `DiscoveryInterval` must be at least 10ms
- `DiscoveryInterval` must not exceed 60 seconds
- **Error**: "DiscoveryInterval must be between 10ms and 60 seconds."

### Timeout Validation
- `Timeout` must be greater than `DiscoveryInterval`
- `Timeout` must be at least 100ms
- **Warning**: If `Timeout` exceeds 60 seconds, a warning is logged (but no error thrown)
- **Error**: "Gossip.Timeout must be greater than DiscoveryInterval."

## Resilience Options Rules

### KeepAliveInterval Validation
- If not `Timeout.InfiniteTimeSpan`, `KeepAliveInterval` must be at least 1 second
- **Error**: "KeepAliveInterval must be at least 1 second or Timeout.InfiniteTimeSpan."

### KeepAliveTimeout Validation
- If both are specified, `KeepAliveTimeout` must be less than `KeepAliveInterval`
- **Error**: "KeepAliveTimeout must be less than KeepAliveInterval."

### RetryOptions Validation
- If retry is enabled (`Retry.Enabled` is `true`):
  - `MaxAttempts` must be greater than 0 or -1 for infinite retries
  - `BackoffMultiplier` must be greater than or equal to 1.0
  - `InitialBackoff` must be at least 10ms
  - `RetryableStatusCodes` must not be empty
- **Error**: "When retry is enabled, MaxAttempts must be greater than 0 or -1 for infinite retries."
- **Error**: "BackoffMultiplier must be greater than or equal to 1.0."
- **Error**: "InitialBackoff must be at least 10ms."
- **Error**: "RetryableStatusCodes cannot be empty when retry is enabled."

### Deadline Consistency
- **Warning**: If `Deadline` is less than `KeepAliveTimeout`, a warning is logged (but no error thrown)

## Security Options Rules

### Transport/Authentication Consistency
- If using `BasicUserCredentials` (username/password), transport security (`Transport.IsEnabled`) should be enabled
- **Warning**: "Using basic authentication without TLS is not secure."
- **Warning**: If using insecure transport with any authentication, a warning is logged

### Certificate Path Validation
- If using `FileCertificateTls` or `ClientFileCertificateCredentials`, paths should be absolute
- **Warning**: "Relative certificate paths may not resolve correctly at runtime."

## Schema Options Rules

### Schema Configuration Consistency
- If `AutoRegister` is `false` and `Validate` is `true`, a warning is logged
- **Warning**: "Schema validation is enabled but auto-registration is disabled, which may lead to validation errors for unregistered schemas."

## General Rules

### Connection Name Validation
- Connection name should not be empty
- Connection name should have reasonable length (less than 256 characters)
- **Error**: "Connection name cannot be empty."
- **Warning**: If connection name exceeds 256 characters, a warning is logged

## Implementation Notes

The `EnsureValid()` method in `KurrentClientOptions` enforces these rules by:

1. Validating critical connection parameters first (scheme, endpoints)
2. Validating nested option objects in a hierarchical fashion
3. Throwing detailed exceptions for validation failures
4. Logging warnings for non-critical issues

Example implementation pattern:
```csharp
public KurrentClientOptions EnsureValid() {
    // Connection validation
    ValidateConnection();
    
    // Endpoint validation
    ValidateEndpoints();
    
    // Gossip options validation
    ValidateGossipOptions();
    
    // Resilience options validation
    ValidateResilienceOptions();
    
    // Security options validation
    ValidateSecurityOptions();
    
    // Schema options validation
    ValidateSchemaOptions();
    
    // General validation
    ValidateGeneral();
    
    return this;
}
```

## Best Practices for Users

When configuring `KurrentClientOptions`:

1. Use the builder pattern via `KurrentClientOptionsBuilder` to ensure proper configuration
2. Set appropriate values for your environment (development vs. production)
3. Use predefined configurations as starting points (`Default`, `HighAvailability`, etc.)
4. Call `EnsureValid()` explicitly if manually constructing options
5. Check for warnings in logs even when no exceptions are thrown
