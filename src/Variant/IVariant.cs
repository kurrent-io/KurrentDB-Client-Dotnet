// ReSharper disable CheckNamespace

namespace Kurrent.Variant {
    /// <summary>
    /// Base interface for the Variant discriminated union type.
    /// </summary>
    public interface IVariant {
        /// <summary>
        /// Gets the current value stored in the union.
        /// </summary>
        object Value { get; }

        /// <summary>
        /// Gets the 0-based index of the current type stored in the union.
        /// </summary>
        int Index { get; }
    }

    public interface IVariant<T0> : IVariant;
    public interface IVariant<T0, T1> : IVariant;
    public interface IVariant<T0, T1, T2> : IVariant;
    public interface IVariant<T0, T1, T2, T3> : IVariant;
    public interface IVariant<T0, T1, T2, T3, T4> : IVariant;
    public interface IVariant<T0, T1, T2, T3, T4, T5> : IVariant;
    public interface IVariant<T0, T1, T2, T3, T4, T5, T6> : IVariant;
    public interface IVariant<T0, T1, T2, T3, T4, T5, T6, T7> : IVariant;
    public interface IVariant<T0, T1, T2, T3, T4, T5, T6, T7, T8> : IVariant;
    public interface IVariant<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> : IVariant;
}
