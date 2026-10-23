using System.Runtime.InteropServices;

namespace Axit.Core;

/// <summary>Windows' own order of names (docs/axfile/SPEC.md AF-2.2, docs/axmedia/SPEC.md AM-3.2): digits compared as numbers, so that 2 comes before 10, as File Explorer sorts; <c>StrCmpLogicalW</c> of shlwapi. From AxFile.Core on its second use, axMedia (2026-10-22).</summary>
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
