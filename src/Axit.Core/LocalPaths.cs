namespace Axit.Core;

/// <summary>
/// Which paths a menu may ask the file system about as it opens (docs/axit/SPEC.md AX-9.4, AX-9.5): a path on a
/// drive of this computer answers at once, while a network share or a mapped network drive can take the network's
/// timeout to answer, which no menu may wait for (2026-10-23: the recent lists checked every path, and an unreachable
/// share stalled the menu). Pure, tested.
/// </summary>
public static class LocalPaths
{
    /// <summary>Whether the path is on a drive of this computer (fixed, removable, a disc, a RAM disk); false for a share, a mapped network drive, or a path without a root. Asking the drive's kind touches no network.</summary>
    public static bool IsLocal(string path)
    {
        if (path.StartsWith(@"\\", StringComparison.Ordinal))
        {
            return false;
        }

        try
        {
            var root = Path.GetPathRoot(path);
            if (string.IsNullOrEmpty(root))
            {
                return false;
            }

            return new DriveInfo(root).DriveType is DriveType.Fixed or DriveType.Removable or DriveType.CDRom or DriveType.Ram;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    /// <summary>Whether the path still exists, asked through <paramref name="exists"/> of a local path only: a path elsewhere is listed as it is, and found missing when it is chosen.</summary>
    public static bool ExistsQuickly(string path, Func<string, bool> exists) => !IsLocal(path) || exists(path);
}
