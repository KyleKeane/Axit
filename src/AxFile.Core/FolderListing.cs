namespace AxFile.Core;

/// <summary>One entry of a folder (SPEC.md AF-2.2): its name, its full path, whether it is a folder, its size in bytes (0 for a folder) and when it was last written; a drive of This PC is a folder entry marked as a drive (AF-3.10).</summary>
public sealed record Entry(string Name, string Path, bool IsFolder, long Size, DateTime Modified, bool IsDrive = false)
{
    /// <summary>The list item: a file by its name as it is, a folder as "name, folder", a drive as "Local Disk (C:), drive".</summary>
    public string Label => IsDrive ? Name + ", drive" : IsFolder ? Name + ", folder" : Name;
}

/// <summary>What Enter does with an entry (AF-3.1, AF-3.2), decided by its kind and its extension.</summary>
public enum OpenWay
{
    /// <summary>A folder: the list shows it.</summary>
    Folder,

    /// <summary>.md, .markdown, .txt: a new AxDown window.</summary>
    AxDown,

    /// <summary>.json: a new AxJSON window.</summary>
    AxJSON,

    /// <summary>A sound or video file (<see cref="MediaFiles"/>): the player.</summary>
    Play,

    /// <summary>Anything else: the program Windows has for it.</summary>
    Windows,
}

/// <summary>The order of the list (AF-3.8): folders first in each; then by name, by date (newest first) or by size (largest first).</summary>
public enum SortBy
{
    Name,
    Date,
    Size,
}

/// <summary>
/// A folder's entries as the list shows them (SPEC.md AF-2.2): folders first, then files, each group in Windows'
/// natural order, hidden and system entries left out unless asked for; the sort orders and the filter (AF-3.8);
/// what Enter does with an entry; the list's name; the information line; the parent; and the reading of a typed
/// path. Pure apart from the file system reads.
/// </summary>
public static class FolderListing
{
    /// <summary>The level above the drives' roots (AF-3.10), shown in the title and the list's name; not a path.</summary>
    public const string ThisPC = "This PC";

    public static bool IsThisPC(string? folder) => string.Equals(folder, ThisPC, StringComparison.Ordinal);

    /// <summary>The ready drives as the entries of This PC (AF-3.10), in letter order: "Local Disk (C:)", the volume's label or the kind's name, each a folder entry marked as a drive.</summary>
    public static IReadOnlyList<Entry> Drives()
    {
        var drives = new List<Entry>();
        foreach (var drive in DriveInfo.GetDrives())
        {
            string label;
            try
            {
                if (!drive.IsReady)
                {
                    continue;
                }

                label = drive.VolumeLabel;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                continue;
            }

            if (label.Length == 0)
            {
                label = drive.DriveType switch
                {
                    DriveType.Removable => "Removable Disk",
                    DriveType.CDRom => "CD Drive",
                    DriveType.Network => "Network Drive",
                    _ => "Local Disk",
                };
            }

            drives.Add(new Entry($"{label} ({drive.Name.TrimEnd('\\')})", drive.Name, IsFolder: true, Size: 0, Modified: DateTime.MinValue, IsDrive: true));
        }

        return drives;
    }

    /// <summary>The entries of <paramref name="folder"/>, sorted. Throws what the file system throws (a folder gone, access denied).</summary>
    public static IReadOnlyList<Entry> Read(string folder, bool showHidden)
    {
        var entries = new List<Entry>();
        foreach (var info in new DirectoryInfo(folder).EnumerateFileSystemInfos())
        {
            if (!showHidden && (info.Attributes & (FileAttributes.Hidden | FileAttributes.System)) != 0)
            {
                continue;
            }

            var isFolder = (info.Attributes & FileAttributes.Directory) != 0;
            var size = !isFolder && info is FileInfo file ? file.Length : 0;
            entries.Add(new Entry(info.Name, info.FullName, isFolder, size, info.LastWriteTime));
        }

        return Sort(entries);
    }

    /// <summary>Folders first, then files, each group in natural order (2 before 10), the way File Explorer sorts by name.</summary>
    public static IReadOnlyList<Entry> Sort(IEnumerable<Entry> entries) => Sort(entries, SortBy.Name);

    /// <summary>Folders first, then files, each group by name, by date newest first, or by size largest first, ties by name (AF-3.8).</summary>
    public static IReadOnlyList<Entry> Sort(IEnumerable<Entry> entries, SortBy by)
    {
        var groups = entries.OrderByDescending(entry => entry.IsFolder);
        var ordered = by switch
        {
            SortBy.Date => groups.ThenByDescending(entry => entry.Modified),
            SortBy.Size => groups.ThenByDescending(entry => entry.Size),
            _ => groups,
        };
        return ordered.ThenBy(entry => entry.Name, NaturalOrder.Instance).ToList();
    }

    /// <summary>The entries whose name contains <paramref name="text"/>, any case; all of them for an empty text (AF-3.8).</summary>
    public static IReadOnlyList<Entry> Filter(IEnumerable<Entry> entries, string text) =>
        text.Length == 0 ? entries.ToList() : entries.Where(entry => entry.Name.Contains(text, StringComparison.OrdinalIgnoreCase)).ToList();

    /// <summary>The list as shown: the entries that pass the filter, in the chosen order.</summary>
    public static IReadOnlyList<Entry> Arrange(IEnumerable<Entry> entries, SortBy by, string filter) => Sort(Filter(entries, filter), by);

    /// <summary>"name", "date", "size": the order's word for the announcement and the menu.</summary>
    public static string SortName(SortBy by) => by.ToString().ToLowerInvariant();

    public static OpenWay WayOf(Entry entry) => entry.IsFolder ? OpenWay.Folder : WayOf(entry.Path);

    /// <summary>What Enter does with a file, by its extension (AF-3.2).</summary>
    public static OpenWay WayOf(string path)
    {
        var extension = System.IO.Path.GetExtension(path).ToLowerInvariant();
        return extension switch
        {
            ".md" or ".markdown" or ".txt" => OpenWay.AxDown,
            ".json" => OpenWay.AxJSON,
            _ when MediaFiles.IsMedia(path) => OpenWay.Play,
            _ => OpenWay.Windows,
        };
    }

    /// <summary>"Dune, 24 items", "Dune, 1 item", "Dune, empty": the list's accessible name (AF-2.1); with a filter on, "Dune, 3 of 24 items, filter chap" (AF-3.8).</summary>
    public static string ListName(string folder, int count, int total = -1, string filter = "")
    {
        if (filter.Length > 0)
        {
            return $"{FolderName(folder)}, {count} of {total} items, filter {filter}";
        }

        return FolderName(folder) + count switch
        {
            0 => ", empty",
            1 => ", 1 item",
            _ => $", {count} items",
        };
    }

    /// <summary>A folder's name for the title and the list: the last part of the path, or the path itself for a drive's root.</summary>
    public static string FolderName(string folder)
    {
        var name = System.IO.Path.GetFileName(System.IO.Path.TrimEndingDirectorySeparator(folder));
        return name.Length == 0 ? folder : name;
    }

    /// <summary>"report.pdf, 1.2 MB, modified 2026-10-18 14:02" or "docs, folder, modified 2026-10-18 14:02" (AF-2.4); a drive: "Local Disk (C:), 120 GB free of 476 GB".</summary>
    public static string Information(Entry entry)
    {
        if (entry.IsDrive)
        {
            try
            {
                var drive = new DriveInfo(entry.Path);
                return $"{entry.Name}, {FormatSize(drive.AvailableFreeSpace)} free of {FormatSize(drive.TotalSize)}";
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
            {
                return entry.Name + ", drive";
            }
        }

        return $"{entry.Name}, {(entry.IsFolder ? "folder" : FormatSize(entry.Size))}, modified {entry.Modified:yyyy-MM-dd HH:mm}";
    }

    /// <summary>"512 bytes", "12 KB", "1.2 MB", "3.5 GB".</summary>
    public static string FormatSize(long bytes)
    {
        const double kilo = 1024;
        return bytes switch
        {
            1 => "1 byte",
            < 1024 => $"{bytes} bytes",
            < 1024 * 1024 => $"{Math.Round(bytes / kilo)} KB",
            < 1024 * 1024 * 1024 => $"{bytes / (kilo * kilo):0.#} MB",
            _ => $"{bytes / (kilo * kilo * kilo):0.#} GB",
        };
    }

    /// <summary>The folder above; null at a drive's root (AF-3.1).</summary>
    public static string? Parent(string folder) =>
        System.IO.Path.GetDirectoryName(System.IO.Path.TrimEndingDirectorySeparator(folder));

    /// <summary>
    /// A typed path (AF-3.3) with environment variables expanded: the folder to show and, when the path names a file,
    /// the file to select; null when there is no such place.
    /// </summary>
    public static (string Folder, string? File)? Resolve(string text)
    {
        var expanded = Environment.ExpandEnvironmentVariables(text.Trim().Trim('"'));
        if (expanded.Length == 0)
        {
            return null;
        }

        string full;
        try
        {
            full = System.IO.Path.GetFullPath(expanded);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return null;
        }

        // "C:\x\" is the folder C:\x; a drive's root keeps its separator.
        if (System.IO.Path.GetPathRoot(full) != full)
        {
            full = System.IO.Path.TrimEndingDirectorySeparator(full);
        }

        if (Directory.Exists(full))
        {
            return (full, null);
        }

        if (File.Exists(full) && System.IO.Path.GetDirectoryName(full) is { } folder)
        {
            return (folder, full);
        }

        return null;
    }
}
