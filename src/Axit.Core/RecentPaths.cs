namespace Axit.Core;

/// <summary>
/// The recent-path lists of the settings files (docs/axit/SPEC.md AX-9.5: the "Open recent …" submenus, the last
/// repository or folder a window starts on): the newest first, each path once whatever its case, at most
/// <see cref="Max"/>. Every app's settings class keeps its own list property, so that its file keeps its shape, and
/// calls here for the two things done to such a list; until 2026-10-23 six classes wrote the same ten lines.
/// </summary>
public static class RecentPaths
{
    /// <summary>What every list is capped at: ten, what a submenu reads comfortably.</summary>
    public const int Max = 10;

    /// <summary>Puts the path at the front of the list, once, and keeps the list to <paramref name="max"/>.</summary>
    public static void Remember(List<string> paths, string path, int max = Max)
    {
        paths.RemoveAll(existing => string.Equals(existing, path, StringComparison.OrdinalIgnoreCase));
        paths.Insert(0, path);
        Trim(paths, max);
    }

    /// <summary>A list as loaded from the file: null becomes empty, blank entries go, and the list is kept to <paramref name="max"/>. Returns the list to assign back.</summary>
    public static List<string> Sanitize(List<string>? paths, int max = Max)
    {
        var list = paths ?? [];
        list.RemoveAll(string.IsNullOrWhiteSpace);
        Trim(list, max);
        return list;
    }

    private static void Trim(List<string> paths, int max)
    {
        if (paths.Count > max)
        {
            paths.RemoveRange(max, paths.Count - max);
        }
    }
}
