// ReSharper disable CheckNamespace

using Kurrent;

namespace Kurrent.Variant;

/// <summary>
/// Specialized variant interface for discriminated union error types where all variants implement IResultError.
/// This interface combines variant capabilities with automatic IResultError passthrough functionality.
/// When accessed as IResultError, the methods delegate to the currently contained error type.
/// </summary>
/// <typeparam name="T0">First error type that must implement IResultError</typeparam>
[PublicAPI]
public interface IVariantResultError<T0> : IVariant<T0>, IResultError 
    where T0 : IResultError;

/// <summary>
/// Specialized variant interface for discriminated union error types where all variants implement IResultError.
/// This interface combines variant capabilities with automatic IResultError passthrough functionality.
/// When accessed as IResultError, the methods delegate to the currently contained error type.
/// </summary>
/// <typeparam name="T0">First error type that must implement IResultError</typeparam>
/// <typeparam name="T1">Second error type that must implement IResultError</typeparam>
[PublicAPI]
public interface IVariantResultError<T0, T1> : IVariant<T0, T1>, IResultError 
    where T0 : IResultError 
    where T1 : IResultError;

/// <summary>
/// Specialized variant interface for discriminated union error types where all variants implement IResultError.
/// This interface combines variant capabilities with automatic IResultError passthrough functionality.
/// When accessed as IResultError, the methods delegate to the currently contained error type.
/// </summary>
/// <typeparam name="T0">First error type that must implement IResultError</typeparam>
/// <typeparam name="T1">Second error type that must implement IResultError</typeparam>
/// <typeparam name="T2">Third error type that must implement IResultError</typeparam>
[PublicAPI]
public interface IVariantResultError<T0, T1, T2> : IVariant<T0, T1, T2>, IResultError 
    where T0 : IResultError 
    where T1 : IResultError 
    where T2 : IResultError;

/// <summary>
/// Specialized variant interface for discriminated union error types where all variants implement IResultError.
/// This interface combines variant capabilities with automatic IResultError passthrough functionality.
/// When accessed as IResultError, the methods delegate to the currently contained error type.
/// </summary>
/// <typeparam name="T0">First error type that must implement IResultError</typeparam>
/// <typeparam name="T1">Second error type that must implement IResultError</typeparam>
/// <typeparam name="T2">Third error type that must implement IResultError</typeparam>
/// <typeparam name="T3">Fourth error type that must implement IResultError</typeparam>
[PublicAPI]
public interface IVariantResultError<T0, T1, T2, T3> : IVariant<T0, T1, T2, T3>, IResultError 
    where T0 : IResultError 
    where T1 : IResultError 
    where T2 : IResultError 
    where T3 : IResultError;

/// <summary>
/// Specialized variant interface for discriminated union error types where all variants implement IResultError.
/// This interface combines variant capabilities with automatic IResultError passthrough functionality.
/// When accessed as IResultError, the methods delegate to the currently contained error type.
/// </summary>
/// <typeparam name="T0">First error type that must implement IResultError</typeparam>
/// <typeparam name="T1">Second error type that must implement IResultError</typeparam>
/// <typeparam name="T2">Third error type that must implement IResultError</typeparam>
/// <typeparam name="T3">Fourth error type that must implement IResultError</typeparam>
/// <typeparam name="T4">Fifth error type that must implement IResultError</typeparam>
[PublicAPI]
public interface IVariantResultError<T0, T1, T2, T3, T4> : IVariant<T0, T1, T2, T3, T4>, IResultError 
    where T0 : IResultError 
    where T1 : IResultError 
    where T2 : IResultError 
    where T3 : IResultError 
    where T4 : IResultError;

/// <summary>
/// Specialized variant interface for discriminated union error types where all variants implement IResultError.
/// This interface combines variant capabilities with automatic IResultError passthrough functionality.
/// When accessed as IResultError, the methods delegate to the currently contained error type.
/// </summary>
/// <typeparam name="T0">First error type that must implement IResultError</typeparam>
/// <typeparam name="T1">Second error type that must implement IResultError</typeparam>
/// <typeparam name="T2">Third error type that must implement IResultError</typeparam>
/// <typeparam name="T3">Fourth error type that must implement IResultError</typeparam>
/// <typeparam name="T4">Fifth error type that must implement IResultError</typeparam>
/// <typeparam name="T5">Sixth error type that must implement IResultError</typeparam>
[PublicAPI]
public interface IVariantResultError<T0, T1, T2, T3, T4, T5> : IVariant<T0, T1, T2, T3, T4, T5>, IResultError 
    where T0 : IResultError 
    where T1 : IResultError 
    where T2 : IResultError 
    where T3 : IResultError 
    where T4 : IResultError 
    where T5 : IResultError;

/// <summary>
/// Specialized variant interface for discriminated union error types where all variants implement IResultError.
/// This interface combines variant capabilities with automatic IResultError passthrough functionality.
/// When accessed as IResultError, the methods delegate to the currently contained error type.
/// </summary>
/// <typeparam name="T0">First error type that must implement IResultError</typeparam>
/// <typeparam name="T1">Second error type that must implement IResultError</typeparam>
/// <typeparam name="T2">Third error type that must implement IResultError</typeparam>
/// <typeparam name="T3">Fourth error type that must implement IResultError</typeparam>
/// <typeparam name="T4">Fifth error type that must implement IResultError</typeparam>
/// <typeparam name="T5">Sixth error type that must implement IResultError</typeparam>
/// <typeparam name="T6">Seventh error type that must implement IResultError</typeparam>
[PublicAPI]
public interface IVariantResultError<T0, T1, T2, T3, T4, T5, T6> : IVariant<T0, T1, T2, T3, T4, T5, T6>, IResultError 
    where T0 : IResultError 
    where T1 : IResultError 
    where T2 : IResultError 
    where T3 : IResultError 
    where T4 : IResultError 
    where T5 : IResultError 
    where T6 : IResultError;

/// <summary>
/// Specialized variant interface for discriminated union error types where all variants implement IResultError.
/// This interface combines variant capabilities with automatic IResultError passthrough functionality.
/// When accessed as IResultError, the methods delegate to the currently contained error type.
/// </summary>
/// <typeparam name="T0">First error type that must implement IResultError</typeparam>
/// <typeparam name="T1">Second error type that must implement IResultError</typeparam>
/// <typeparam name="T2">Third error type that must implement IResultError</typeparam>
/// <typeparam name="T3">Fourth error type that must implement IResultError</typeparam>
/// <typeparam name="T4">Fifth error type that must implement IResultError</typeparam>
/// <typeparam name="T5">Sixth error type that must implement IResultError</typeparam>
/// <typeparam name="T6">Seventh error type that must implement IResultError</typeparam>
/// <typeparam name="T7">Eighth error type that must implement IResultError</typeparam>
[PublicAPI]
public interface IVariantResultError<T0, T1, T2, T3, T4, T5, T6, T7> : IVariant<T0, T1, T2, T3, T4, T5, T6, T7>, IResultError 
    where T0 : IResultError 
    where T1 : IResultError 
    where T2 : IResultError 
    where T3 : IResultError 
    where T4 : IResultError 
    where T5 : IResultError 
    where T6 : IResultError 
    where T7 : IResultError;

/// <summary>
/// Specialized variant interface for discriminated union error types where all variants implement IResultError.
/// This interface combines variant capabilities with automatic IResultError passthrough functionality.
/// When accessed as IResultError, the methods delegate to the currently contained error type.
/// </summary>
/// <typeparam name="T0">First error type that must implement IResultError</typeparam>
/// <typeparam name="T1">Second error type that must implement IResultError</typeparam>
/// <typeparam name="T2">Third error type that must implement IResultError</typeparam>
/// <typeparam name="T3">Fourth error type that must implement IResultError</typeparam>
/// <typeparam name="T4">Fifth error type that must implement IResultError</typeparam>
/// <typeparam name="T5">Sixth error type that must implement IResultError</typeparam>
/// <typeparam name="T6">Seventh error type that must implement IResultError</typeparam>
/// <typeparam name="T7">Eighth error type that must implement IResultError</typeparam>
/// <typeparam name="T8">Ninth error type that must implement IResultError</typeparam>
[PublicAPI]
public interface IVariantResultError<T0, T1, T2, T3, T4, T5, T6, T7, T8> : IVariant<T0, T1, T2, T3, T4, T5, T6, T7, T8>, IResultError 
    where T0 : IResultError 
    where T1 : IResultError 
    where T2 : IResultError 
    where T3 : IResultError 
    where T4 : IResultError 
    where T5 : IResultError 
    where T6 : IResultError 
    where T7 : IResultError 
    where T8 : IResultError;

/// <summary>
/// Specialized variant interface for discriminated union error types where all variants implement IResultError.
/// This interface combines variant capabilities with automatic IResultError passthrough functionality.
/// When accessed as IResultError, the methods delegate to the currently contained error type.
/// </summary>
/// <typeparam name="T0">First error type that must implement IResultError</typeparam>
/// <typeparam name="T1">Second error type that must implement IResultError</typeparam>
/// <typeparam name="T2">Third error type that must implement IResultError</typeparam>
/// <typeparam name="T3">Fourth error type that must implement IResultError</typeparam>
/// <typeparam name="T4">Fifth error type that must implement IResultError</typeparam>
/// <typeparam name="T5">Sixth error type that must implement IResultError</typeparam>
/// <typeparam name="T6">Seventh error type that must implement IResultError</typeparam>
/// <typeparam name="T7">Eighth error type that must implement IResultError</typeparam>
/// <typeparam name="T8">Ninth error type that must implement IResultError</typeparam>
/// <typeparam name="T9">Tenth error type that must implement IResultError</typeparam>
[PublicAPI]
public interface IVariantResultError<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> : IVariant<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>, IResultError 
    where T0 : IResultError 
    where T1 : IResultError 
    where T2 : IResultError 
    where T3 : IResultError 
    where T4 : IResultError 
    where T5 : IResultError 
    where T6 : IResultError 
    where T7 : IResultError 
    where T8 : IResultError 
    where T9 : IResultError;