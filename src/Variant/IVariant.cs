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

    /// <summary>
    /// Base struct for efficient Variant implementations when all types are reference types.
    /// This provides better performance than object-based storage for certain scenarios.
    /// </summary>
    public readonly struct VariantValue<T>(T? value, int index) where T : class? {
        public T?   Value { get; } = value;
        public int  Index { get; } = index;

        public bool HasValue => Value is not null;
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
