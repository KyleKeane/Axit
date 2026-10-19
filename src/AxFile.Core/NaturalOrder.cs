using System.Runtime.InteropServices;

namespace AxFile.Core;

/// <summary>Windows' own order of names (SPEC.md AF-2.2): digits compared as numbers, so that 2 comes before 10, as File Explorer sorts; <c>StrCmpLogicalW</c> of shlwapi.</summary>
public sealed class NaturalOrder : IComparer<string>
{
    public static readonly NaturalOrder Instance = new();

    private NaturalOrder()
    {
    }

    public int Compare(string? x, string? y) => StrCmpLogicalW(x ?? string.Empty, y ?? string.Empty);

    [DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
    private static extern int StrCmpLogicalW(string first, string second);
}
