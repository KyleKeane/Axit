using System.IO.Compression;

namespace AxFile.Core;

/// <summary>
/// Zip files the window makes and opens (SPEC.md AF-4.12), through .NET's own <see cref="ZipFile"/>, no package: the
/// entries compressed into a new zip in their folder, and a zip extracted into a new folder next to it. Both pick a
/// name that is free, "name - 2.zip" (<see cref="FileOperations.FreeName"/>), rather than write over anything, and
/// both count their files for the Actions list's line (AF-9.2) through the <c>progress</c> callback, called on the
/// caller's thread after each file with the files done and the total.
/// </summary>
public static class Archives
{
    public static bool IsZip(string path) => string.Equals(System.IO.Path.GetExtension(path), ".zip", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Compresses files and folders into a new zip in <paramref name="folder"/>, named after the one entry without
    /// its extension, or after the folder for several; a folder's contents go under its name, an empty folder as an
    /// entry of its own. The name is taken at once with an empty file, so that a second compress running meanwhile
    /// picks the next one (AF-4.5); the zip is written to a temporary name and moved over it. Returns the zip's path.
    /// </summary>
    public static string Compress(IReadOnlyList<string> sources, string folder, Action<int, int>? progress = null)
    {
        var stem = sources.Count == 1 ? System.IO.Path.GetFileNameWithoutExtension(sources[0]) : Axit.Core.PathName.Of(folder);
        var zip = FileOperations.FreeName(folder, stem.Length == 0 ? "archive" : stem, ".zip");
        File.WriteAllBytes(zip, []);
        var temporary = zip + ".tmp";
        var total = sources.Sum(CountFiles);
        var done = 0;
        var written = false;
        try
        {
            using (var archive = ZipFile.Open(temporary, ZipArchiveMode.Create))
            {
                foreach (var source in sources)
                {
                    Add(archive, source, System.IO.Path.GetFileName(System.IO.Path.TrimEndingDirectorySeparator(source)), () => progress?.Invoke(++done, total));
                }
            }

            File.Move(temporary, zip, overwrite: true);
            written = true;
        }
        finally
        {
            if (File.Exists(temporary))
            {
                File.Delete(temporary);
            }

            if (!written && File.Exists(zip))
            {
                File.Delete(zip);
            }
        }

        return zip;
    }

    private static int CountFiles(string path) =>
        Directory.Exists(path) ? Directory.EnumerateFileSystemEntries(path).Where(child => !IsLink(child)).Sum(CountFiles) : 1;

    /// <summary>
    /// A link or a junction inside a folder being compressed: left out, never followed, since it can lead anywhere,
    /// into the user's own private folders or to a share (security audit S-8). A link the user chose itself is kept.
    /// </summary>
    private static bool IsLink(string path) => (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0;

    private static void Add(ZipArchive archive, string path, string name, Action fileDone)
    {
        if (!Directory.Exists(path))
        {
            archive.CreateEntryFromFile(path, name, CompressionLevel.Optimal);
            fileDone();
            return;
        }

        var any = false;
        foreach (var child in Directory.EnumerateFileSystemEntries(path).Where(child => !IsLink(child)))
        {
            Add(archive, child, name + "/" + System.IO.Path.GetFileName(child), fileDone);
            any = true;
        }

        if (!any)
        {
            archive.CreateEntry(name + "/");
        }
    }

    /// <summary>
    /// Extracts a zip into a new folder in <paramref name="folder"/> named after the zip, "name - 2" when taken, so
    /// that nothing is written over; an entry that would land outside the folder is refused. Returns the folder's
    /// path.
    /// </summary>
    public static string Extract(string zip, string folder, Action<int, int>? progress = null)
    {
        var stem = System.IO.Path.GetFileNameWithoutExtension(zip);
        var target = FileOperations.FreeName(folder, stem.Length == 0 ? "archive" : stem, string.Empty);
        using var archive = ZipFile.OpenRead(zip);
        var root = System.IO.Path.GetFullPath(target + System.IO.Path.DirectorySeparatorChar);
        var total = archive.Entries.Count(entry => !IsFolderEntry(entry));
        var done = 0;
        var zone = DownloadMark(zip);
        Directory.CreateDirectory(target);
        foreach (var entry in archive.Entries)
        {
            var destination = System.IO.Path.GetFullPath(System.IO.Path.Combine(target, entry.FullName.Replace('/', System.IO.Path.DirectorySeparatorChar)));
            if (!destination.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            {
                throw new IOException($"{entry.FullName} would land outside the folder");
            }

            if (IsFolderEntry(entry))
            {
                Directory.CreateDirectory(destination);
                continue;
            }

            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(destination)!);
            entry.ExtractToFile(destination, overwrite: false);
            if (zone is not null)
            {
                MarkAsDownloaded(destination, zone);
            }

            progress?.Invoke(++done, total);
        }

        return target;
    }

    /// <summary>
    /// The zip's mark of the web (its <c>Zone.Identifier</c> stream), which Windows puts on a downloaded file; null
    /// when there is none or the drive keeps no such streams. Each extracted file gets it too, as File Explorer does,
    /// so that SmartScreen and Office's Protected View still warn about a file that came from the internet (security
    /// audit S-7).
    /// </summary>
    public static string? DownloadMark(string path)
    {
        try
        {
            var mark = path + ":Zone.Identifier";
            return File.Exists(mark) ? File.ReadAllText(mark) : null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            return null;
        }
    }

    private static void MarkAsDownloaded(string path, string zone)
    {
        try
        {
            File.WriteAllText(path + ":Zone.Identifier", zone);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            // A drive without streams (FAT, a share that refuses them): the file stays unmarked, as Explorer leaves it.
        }
    }

    /// <summary>A folder entry ends in a slash; older Windows tools wrote a backslash.</summary>
    private static bool IsFolderEntry(ZipArchiveEntry entry) => entry.FullName.EndsWith('/') || entry.FullName.EndsWith('\\');
}
