using System.Collections.Concurrent;
using System.Runtime.ExceptionServices;
using Google.Protobuf.Reflection;
using Kurrent.Client.SchemaRegistry.Serialization.Protobuf;
using Kurrent.Variant;
using KurrentDB.Protocol;

namespace Kurrent.Client.Model;

public interface IKurrentOperationError : IResultError;

/// <summary>
/// Represents a contract for Kurrent client-specific errors.
/// Extends IResultError with default KurrentClientException creation behavior.
/// </summary>
[PublicAPI]
public interface IKurrentClientError : IResultError {
    /// <summary>
    /// Default implementation creates a KurrentClientException.
    /// This can be overridden by implementing types if different exception behavior is needed.
    /// </summary>
    Exception IResultError.CreateException(Exception? innerException) =>
        new KurrentClientException(ErrorCode, ErrorMessage, null, innerException);
}
