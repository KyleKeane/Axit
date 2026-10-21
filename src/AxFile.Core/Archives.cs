using System.IO.Compression;

namespace AxFile.Core;

/// <summary>
/// Zip files the window makes and opens (SPEC.md AF-4.12), through .NET's own <see cref="ZipFile"/>, no package: the
/// entries compressed into a new zip in their folder, and a zip extracted into a new folder next to it. Both pick a
/// name that is free, "name (2).zip", rather than write over anything.
/// </summary>
public static class Archives
{
    public static bool IsZip(string path) => string.Equals(System.IO.Path.GetExtension(path), ".zip", StringComparison.OrdinalIgnoreCase);

    /// <summary>"name.zip", or "name (2).zip", "name (3).zip" when taken: the first path in <paramref name="folder"/> that nothing has.</summary>
    public static string FreeName(string folder, string stem, string extension)
    {
        for (var n = 1; ; n++)
        {
            var name = n == 1 ? stem + extension : $"{stem} ({n}){extension}";
            var path = System.IO.Path.Combine(folder, name);
            if (!File.Exists(path) && !Directory.Exists(path))
            {
                return path;
            }
        }
    }

    /// <summary>
    /// Compresses files and folders into a new zip in <paramref name="folder"/>, named after the one entry without
    /// its extension, or after the folder for several; a folder's contents go under its name, an empty folder as an
    /// entry of its own. Written to a temporary name and moved into place. Returns the zip's path.
    /// </summary>
    public static string Compress(IReadOnlyList<string> sources, string folder)
    {
        var stem = sources.Count == 1 ? System.IO.Path.GetFileNameWithoutExtension(sources[0]) : FolderListing.FolderName(folder);
        var zip = FreeName(folder, stem.Length == 0 ? "archive" : stem, ".zip");
        var temporary = zip + ".tmp";
        try
        {
            using (var archive = ZipFile.Open(temporary, ZipArchiveMode.Create))
            {
                foreach (var source in sources)
                {
                    Add(archive, source, System.IO.Path.GetFileName(System.IO.Path.TrimEndingDirectorySeparator(source)));
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

    private static void Add(ZipArchive archive, string path, string name)
    {
        if (!Directory.Exists(path))
        {
            archive.CreateEntryFromFile(path, name, CompressionLevel.Optimal);
            return;
        }

        var any = false;
        foreach (var child in Directory.EnumerateFileSystemEntries(path))
        {
            Add(archive, child, name + "/" + System.IO.Path.GetFileName(child));
            any = true;
        }

        if (!any)
        {
            archive.CreateEntry(name + "/");
        }
    }

    /// <summary>
    /// Extracts a zip into a new folder in <paramref name="folder"/> named after the zip, "name (2)" when taken, so
    /// that nothing is written over; an entry that would land outside the folder is refused by the framework.
    /// Returns the folder's path.
    /// </summary>
    public static string Extract(string zip, string folder)
    {
        var stem = System.IO.Path.GetFileNameWithoutExtension(zip);
        var target = FreeName(folder, stem.Length == 0 ? "archive" : stem, string.Empty);
        ZipFile.ExtractToDirectory(zip, target);
        return target;
    }
}
