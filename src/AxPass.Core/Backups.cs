using System.Globalization;

namespace AxPass.Core;

/// <summary>
/// Copies of the database kept against a corrupted or wrongly changed file (docs/axpass/SPEC.md PW-4.5, the user's
/// ask of 2026-10-24): the <c>.kdbx</c> file as it is, encrypted as it is, copied into a folder beside it ("mine
/// backups" next to mine.kdbx, the user's choice, so that a synced database's backups sync too), named with the time,
/// the newest <see cref="Keep"/> kept. A backup opens like any database, with the same master password.
/// </summary>
public static class Backups
{
    public const int Keep = 20;

    private const string TimeFormat = "yyyy-MM-dd HH-mm-ss";

    /// <summary>"C:\Vault\mine backups" for C:\Vault\mine.kdbx.</summary>
    public static string FolderFor(string database) =>
        Path.Combine(Path.GetDirectoryName(Path.GetFullPath(database))!, PassTree.DatabaseName(database) + " backups");

    /// <summary>The database copied into its backup folder as "mine 2026-10-24 15-30-05.kdbx", the oldest copies past <see cref="Keep"/> deleted; the copy's path.</summary>
    public static string Make(string database, DateTime now)
    {
        var folder = FolderFor(database);
        Directory.CreateDirectory(folder);
        var stem = PassTree.DatabaseName(database) + " " + now.ToString(TimeFormat, CultureInfo.InvariantCulture);
        var path = Path.Combine(folder, stem + ".kdbx");
        for (var n = 2; File.Exists(path); n++)
        {
            path = Path.Combine(folder, $"{stem} - {n}.kdbx");
        }

        File.Copy(database, path);
        foreach (var old in List(database).Skip(Keep))
        {
            File.Delete(old);
        }

        return path;
    }

    /// <summary>The backups of a database, the newest first.</summary>
    public static IReadOnlyList<string> List(string database)
    {
        var folder = FolderFor(database);
        if (!Directory.Exists(folder))
        {
            return [];
        }

        var prefix = PassTree.DatabaseName(database) + " ";
        return Directory.GetFiles(folder, "*.kdbx")
            .Where(file => Path.GetFileName(file).StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            // The time is in the name, so the names' order is the backups' (a copy keeps the database's own write time).
            .OrderByDescending(file => Path.GetFileName(file), StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>Whether a command of the tool changes the database, so that a backup comes first.</summary>
    public static bool Changes(string command) => command is "add" or "edit" or "rm" or "rmdir" or "mkdir" or "mv";
}
