using System.IO.Compression;

namespace AxFile.Core;

/// <summary>
/// Zip files the window makes and opens (SPEC.md AF-4.12), through .NET's own <see cref="ZipFile"/>, no package: the
/// entries compressed into a new zip in their folder, and a zip extracted into a new folder next to it. Both pick a
/// name that is free, "name - 2.zip" (<see cref="FileOperations.FreeName"/>), rather than write over anything, and
/// both count their files for the pane's status line (AF-9.3) through the <c>progress</c> callback, called on the
/// caller's thread after each file with the files done and the total.
/// </summary>
public static class Archives
{
    public static bool IsZip(string path) => string.Equals(System.IO.Path.GetExtension(path), ".zip", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Compresses files and folders into a new zip in <paramref name="folder"/>, named after the one entry without
    /// its extension, or after the folder for several; a folder's contents go under its name, an empty folder as an
    /// entry of its own. Written to a temporary name and moved into place. Returns the zip's path.
    /// </summary>
    public static string Compress(IReadOnlyList<string> sources, string folder, Action<int, int>? progress = null)
    {
        var stem = sources.Count == 1 ? System.IO.Path.GetFileNameWithoutExtension(sources[0]) : FolderListing.FolderName(folder);
        var zip = FileOperations.FreeName(folder, stem.Length == 0 ? "archive" : stem, ".zip");
        var temporary = zip + ".tmp";
        var total = sources.Sum(CountFiles);
        var done = 0;
        try
        {
            using (var archive = ZipFile.Open(temporary, ZipArchiveMode.Create))
            {
                foreach (var source in sources)
                {
                    Add(archive, source, System.IO.Path.GetFileName(System.IO.Path.TrimEndingDirectorySeparator(source)), () => progress?.Invoke(++done, total));
                }
            }

            File.Move(temporary, zip);
        }
        finally
        {
            if (File.Exists(temporary))
            {
                File.Delete(temporary);
            }
        }

        return zip;
    }

    private static int CountFiles(string path) =>
        Directory.Exists(path) ? Directory.EnumerateFileSystemEntries(path).Sum(CountFiles) : 1;

    private static void Add(ZipArchive archive, string path, string name, Action fileDone)
    {
        if (!Directory.Exists(path))
        {
            archive.CreateEntryFromFile(path, name, CompressionLevel.Optimal);
            fileDone();
            return;
        }

        var any = false;
        foreach (var child in Directory.EnumerateFileSystemEntries(path))
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
        var files = archive.Entries.Where(entry => !entry.FullName.EndsWith('/')).ToList();
        var total = files.Count;
        var done = 0;
        Directory.CreateDirectory(target);
        foreach (var entry in archive.Entries)
        {
            var destination = System.IO.Path.GetFullPath(System.IO.Path.Combine(target, entry.FullName));
            if (!destination.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            {
                throw new IOException($"{entry.FullName} would land outside the folder");
            }

            if (entry.FullName.EndsWith('/'))
            {
                Directory.CreateDirectory(destination);
                continue;
            }

            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(destination)!);
            entry.ExtractToFile(destination, overwrite: false);
            progress?.Invoke(++done, total);
        }

        return target;
    }
}
