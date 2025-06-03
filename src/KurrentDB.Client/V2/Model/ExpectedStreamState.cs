// ReSharper disable CheckNamespace

using System.Runtime.InteropServices;
using Kurrent.Client.Model;
using OneOf;

namespace Kurrent.Client;

[PublicAPI]
[GenerateOneOf]
public partial class ExpectedStreamState : OneOfBase<StreamRevision, ExpectedStreamState.NoStream, ExpectedStreamState.StreamExists, ExpectedStreamState.Any> {
	public bool IsStreamRevision => IsT0;
	public bool IsNoStream       => IsT1;
	public bool IsStreamExists   => IsT2;
	public bool IsAny            => IsT3;

	public StreamRevision AsStreamRevision() => AsT0;
	public NoStream       AsNoStream()       => AsT1;
	public StreamExists   AsStreamExists()   => AsT2;
	public Any            AsAny()            => AsT3;

	public static implicit operator ExpectedStreamState(long _) => StreamRevision.From(_);
	public static implicit operator long(ExpectedStreamState _) => _.IsStreamRevision ? _.AsStreamRevision().Value : StreamRevision.Unset;

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public readonly record struct NoStream {
		public static readonly NoStream Instance = new();
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public readonly record struct Any {
		public static readonly NoStream Instance = new();
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public readonly record struct StreamExists {
		public static readonly NoStream Instance = new();
	}
}
