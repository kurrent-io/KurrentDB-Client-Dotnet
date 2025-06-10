#nullable enable

namespace Kurrent.Whatever
{
    /// <summary>
    /// Base interface for the Whatever discriminated union type.
    /// </summary>
    public interface IWhatever
    {
        /// <summary>
        /// Gets the current value stored in the union.
        /// </summary>
        object Value { get; }

        /// <summary>
        /// Gets the 0-based index of the current type stored in the union.
        /// </summary>
        int Index { get; }
    }

    public interface IWhatever<T0> : IWhatever { }
    public interface IWhatever<T0, T1> : IWhatever { }
    public interface IWhatever<T0, T1, T2> : IWhatever { }
    public interface IWhatever<T0, T1, T2, T3> : IWhatever { }
    public interface IWhatever<T0, T1, T2, T3, T4> : IWhatever { }
    public interface IWhatever<T0, T1, T2, T3, T4, T5> : IWhatever { }
    public interface IWhatever<T0, T1, T2, T3, T4, T5, T6> : IWhatever { }
    public interface IWhatever<T0, T1, T2, T3, T4, T5, T6, T7> : IWhatever { }
    public interface IWhatever<T0, T1, T2, T3, T4, T5, T6, T7, T8> : IWhatever { }
    public interface IWhatever<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> : IWhatever { }
}
