using System.Runtime.InteropServices;

namespace Kurrent.Client;

[StructLayout(LayoutKind.Sequential, Size = 1)]
public readonly struct Success {
    public static readonly Success Instance = new();
}

public static class Results {
    public static Success Success => Success.Instance;
}
