using System.IO.Compression;
using Axit.Core;

namespace AxPreview.Core;

/// <summary>
/// A zip archive's listing (SPEC.md AP-7.2, 3): one entry to a line, its path, its unpacked size and its date
/// ("docs/SPEC.md, 41 KB, 2026-10-24 14:02"), in File Explorer's order of names; a folder entry as "docs/, folder".
/// Only the archive's directory is read, never what it packs. Tested in <c>tests/AxPreview.Tests/RenderersTests</c>.
/// </summary>
public static class ZipListing
{
    public static Rendered Render(string path)
    {
        using var zip = ZipFile.OpenRead(path);
        var entries = zip.Entries.OrderBy(Name, NaturalOrder.Instance).ToList();
        var folders = entries.Count(IsFolder);
        var lines = entries
            .Select(entry => IsFolder(entry)
                ? $"{Name(entry)}, folder"
                : $"{Name(entry)}, {FileSize.Text(entry.Length)}, {entry.LastWriteTime.LocalDateTime:yyyy-MM-dd HH:mm}")
            .ToList();
        if (lines.Count == 0)
        {
            lines.Add($"{Path.GetFileName(path)} is an empty zip archive.");
        }

        return Rendered.Of(lines, "Zip archive",
        [
            "Format: Zip archive",
            $"Entries: {entries.Count:N0}",
            $"Files: {entries.Count - folders:N0}",
            $"Folders: {folders:N0}",
            $"Packed: {FileDetails.Size(entries.Sum(entry => entry.CompressedLength))}",
            $"Unpacked: {FileDetails.Size(entries.Sum(entry => entry.Length))}",
        ], WayOut.Windows);
    }

    /// <summary>An entry's path with forward slashes, as the format has them; Windows PowerShell 5.1 writes backslashes.</summary>
    private static string Name(ZipArchiveEntry entry) => entry.FullName.Replace('\\', '/');

    /// <summary>A folder entry: a name ending in a slash and nothing in it.</summary>
    private static bool IsFolder(ZipArchiveEntry entry) => Name(entry).EndsWith('/') && entry.Length == 0;
}
