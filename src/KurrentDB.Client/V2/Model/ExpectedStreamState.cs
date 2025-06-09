// ReSharper disable CheckNamespace

using System.Runtime.InteropServices;
using Kurrent.Client.Model;
using OneOf;

namespace Kurrent.Client.Model;

public readonly struct ExpectedStreamState {
    /// <summary>
	/// The stream should not exist.
	/// </summary>
	public static readonly ExpectedStreamState NoStream = new(-1);

	/// <summary>
	/// The stream may or may not exist.
	/// </summary>
	public static readonly ExpectedStreamState Any = new(-2);

	/// <summary>
	/// The stream must exist.
	/// </summary>
	public static readonly ExpectedStreamState StreamExists = new(-4);

    public long Value { get; }

    internal ExpectedStreamState(long value) {
        Value = value switch {
            -1 or -2 or -4 or >= 0 => value,
            _                      => throw new ArgumentOutOfRangeException(nameof(value), value, "ExpectedStreamState must be -1, -2, -4, or a positive integer")
        };
	}

    public static implicit operator ExpectedStreamState(long _)           => new(_);
    public static implicit operator long(ExpectedStreamState _)           => _.Value;
    public static implicit operator StreamRevision(ExpectedStreamState _) => _.Value;
    public static implicit operator ulong(ExpectedStreamState _)          => (ulong)_.Value;
}

// [PublicAPI]
// [GenerateOneOf]
// public partial class ExpectedStreamState : OneOfBase<StreamRevision, ExpectedStreamState.NoStream, ExpectedStreamState.StreamExists, ExpectedStreamState.Any> {
// 	public bool IsStreamRevision => IsT0;
// 	public bool IsNoStream       => IsT1;
// 	public bool IsStreamExists   => IsT2;
// 	public bool IsAny            => IsT3;
//
// 	public StreamRevision AsStreamRevision=> AsT0;
// 	public NoStream       AsNoStream       => AsT1;
// 	public StreamExists   AsStreamExists   => AsT2;
// 	public Any            AsAny            => AsT3;
//
// 	public static implicit operator ExpectedStreamState(long _) => StreamRevision.From(_);
// 	public static implicit operator long(ExpectedStreamState _) => _.IsStreamRevision ? _.AsStreamRevision.Value : StreamRevision.Unset;
//
// 	[StructLayout(LayoutKind.Sequential, Size = 1)]
// 	public readonly record struct NoStream {
// 		public static readonly NoStream Value = new();
// 	}
//
// 	[StructLayout(LayoutKind.Sequential, Size = 1)]
// 	public readonly record struct Any {
// 		public static readonly Any Value = new();
// 	}
//
// 	[StructLayout(LayoutKind.Sequential, Size = 1)]
// 	public readonly record struct StreamExists {
// 		public static readonly StreamExists Value = new();
// 	}
// }
