namespace KurrentDB.Client;

/// <summary>
/// The base exception that is thrown when an KurrentDB connection string could not be parsed.
/// </summary>
public class ConnectionStringParseException : Exception {
    /// <summary>
    /// Constructs a new <see cref="ConnectionStringParseException"/>.
    /// </summary>
    /// <param name="message"></param>
    /// <param name="innerException">The exception that is the cause of the current exception, or a null reference (<see langword="Nothing" /> in Visual Basic) if no inner exception is specified.</param>
    public ConnectionStringParseException(string message, Exception? innerException = null) : base(message, innerException) { }
}

/// <summary>
/// The exception that is thrown when a key in the KurrentDB connection string is duplicated.
/// </summary>
public class DuplicateKeyException : ConnectionStringParseException {
    /// <summary>
    /// Constructs a new <see cref="DuplicateKeyException"/>.
    /// </summary>
    /// <param name="key"></param>
    public DuplicateKeyException(string key)
        : base($"Duplicate key: '{key}'") { }
}

/// <summary>
/// The exception that is thrown when a certificate is invalid or not found in the KurrentDB connection string.
/// </summary>
public class InvalidClientCertificateException : ConnectionStringParseException {
    /// <summary>
    /// Constructs a new <see cref="InvalidClientCertificateException"/>.
    /// </summary>
    /// <param name="message"></param>
    /// <param name="innerException"></param>
    public InvalidClientCertificateException(string message, Exception? innerException = null)
        : base(message, innerException) { }
}

/// <summary>
/// The exception that is thrown when there is an invalid host in the KurrentDB connection string.
/// </summary>
public class InvalidHostException : ConnectionStringParseException {
    /// <summary>
    /// Constructs a new <see cref="InvalidHostException"/>.
    /// </summary>
    /// <param name="host"></param>
    public InvalidHostException(string host)
        : base($"Invalid host: '{host}'") { }
}

/// <summary>
/// The exception that is thrown when an invalid key value pair is found in an KurrentDB connection string.
/// </summary>
public class InvalidKeyValuePairException : ConnectionStringParseException {
    /// <summary>
    /// Constructs a new <see cref="InvalidKeyValuePairException"/>.
    /// </summary>
    /// <param name="keyValuePair"></param>
    public InvalidKeyValuePairException(string keyValuePair)
        : base($"Invalid key/value pair: '{keyValuePair}'") { }
}

/// <summary>
/// The exception that is thrown when an invalid scheme is defined in the KurrentDB connection string.
/// </summary>
public class InvalidSchemeException : ConnectionStringParseException {
    /// <summary>
    /// Constructs a new <see cref="InvalidSchemeException"/>.
    /// </summary>
    /// <param name="scheme"></param>
    /// <param name="supportedSchemes"></param>
    public InvalidSchemeException(string scheme, string[] supportedSchemes)
        : base($"Invalid scheme: '{scheme}'. Supported values are: {string.Join(",", supportedSchemes)}") { }
}

/// <summary>
/// The exception that is thrown when an invalid setting is found in an KurrentDB connection string.
/// </summary>
public class InvalidSettingException : ConnectionStringParseException {
    /// <summary>
    /// Constructs a new <see cref="InvalidSettingException"/>.
    /// </summary>
    /// <param name="message"></param>
    public InvalidSettingException(string message) : base(message) { }
}

/// <summary>
/// The exception that is thrown when an invalid <see cref="UserCredentials"/> is specified in the KurrentDB connection string.
/// </summary>
public class InvalidUserCredentialsException : ConnectionStringParseException {
    /// <summary>
    ///
    /// </summary>
    /// <param name="userInfo"></param>
    public InvalidUserCredentialsException(string userInfo)
        : base($"Invalid user credentials: '{userInfo}'. Username & password must be delimited by a colon") { }
}

/// <summary>
/// The exception that is thrown when no scheme was specified in the KurrentDB connection string.
/// </summary>
public class NoSchemeException : ConnectionStringParseException {
    /// <summary>
    /// Constructs a new <see cref="NoSchemeException"/>.
    /// </summary>
    public NoSchemeException()
        : base("Could not parse scheme from connection string") { }
}
