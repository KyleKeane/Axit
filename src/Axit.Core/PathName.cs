namespace Axit.Core;

/// <summary>
/// The name of a path for a title, a label or an announcement (docs/axit/SPEC.md AX-9.5): the last part of the path,
/// a trailing separator ignored, or the path itself for a drive's root, which has no last part ("C:\"). Every window
/// names its folder this way; until 2026-10-23 four classes computed it themselves.
/// </summary>
public static class PathName
{
    public static string Of(string path)
    {
        var name = Path.GetFileName(Path.TrimEndingDirectorySeparator(path));
        return name.Length == 0 ? path : name;
    }
}
