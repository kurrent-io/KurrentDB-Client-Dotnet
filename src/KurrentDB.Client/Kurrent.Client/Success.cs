using System.Runtime.InteropServices;

namespace Kurrent.Client;

[StructLayout(LayoutKind.Sequential, Size = 1)]
public readonly struct Success {
    public static readonly Success Instance = new();
}
