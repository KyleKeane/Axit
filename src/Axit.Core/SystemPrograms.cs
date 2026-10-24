namespace Axit.Core;

/// <summary>
/// The programs the apps start, found so that a file planted where Axit was started can never stand in for them
/// (docs/axit/security-audit-2026-10-24.md S-1). Windows starts a bare name ("git", "cmd.exe") from the current folder
/// before System32 and the PATH, and an app started from Explorer or a console keeps the folder it was started in: a
/// downloaded repository, a folder of downloads. So Windows' own programs are named by their full path, and any other
/// is looked for on the PATH by Axit itself, in folders that are full paths only (a relative entry, "." included,
/// means the current folder). Every app starts its programs through here or by a full path it found itself.
/// </summary>
public static class SystemPrograms
{
    /// <summary>The Windows folder, <c>C:\Windows</c>.</summary>
    public static string Windows => Environment.GetFolderPath(Environment.SpecialFolder.Windows);

    public static string Explorer => Path.Combine(Windows, "explorer.exe");

    public static string Cmd => Path.Combine(Environment.SystemDirectory, "cmd.exe");

    /// <summary>Windows PowerShell 5.1, the one every Windows has.</summary>
    public static string PowerShell => Path.Combine(Environment.SystemDirectory, "WindowsPowerShell", "v1.0", "powershell.exe");

    /// <summary>The first <paramref name="fileName"/> in the PATH's folders that are full paths; null when there is none.</summary>
    public static string? OnPath(string fileName) => OnPath(fileName, Environment.GetEnvironmentVariable("PATH"));

    /// <summary>The same over a given PATH, for the tests.</summary>
    public static string? OnPath(string fileName, string? path)
    {
        foreach (var entry in (path ?? string.Empty).Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var folder = entry.Trim('"');
            try
            {
                if (!Path.IsPathFullyQualified(folder))
                {
                    continue;
                }

                var candidate = Path.Combine(folder, fileName);
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }
            catch (ArgumentException)
            {
                // A PATH entry with characters a path cannot hold: skipped.
            }
        }

        return null;
    }

    /// <summary>The PATH's folders that are full paths, in order: where a program may be looked for.</summary>
    public static IReadOnlyList<string> PathFolders() =>
        (Environment.GetEnvironmentVariable("PATH") ?? string.Empty)
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(entry => entry.Trim('"'))
            .Where(folder => { try { return Path.IsPathFullyQualified(folder); } catch (ArgumentException) { return false; } })
            .ToList();
}
