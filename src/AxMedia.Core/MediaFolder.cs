using Axit.Core;

namespace AxMedia.Core;

/// <summary>
/// What a start with paths, Open folder and Add make of folders and files (SPEC.md AM-1.1, AM-3.2, AM-D2): a folder's
/// media files in name order with the hidden ones left out, a file with its folder's other media files after it. The
/// hand-over is a path, not a list: axMedia reads the folder itself, so that axFile's own order and settings are not
/// carried. Pure apart from the file system reads; tested in <c>tests/AxMedia.Tests</c>.
/// </summary>
public static class MediaFolder
{
    /// <summary>The media files of <paramref name="folder"/> in Windows' natural order, hidden and system files left out; throws what the file system throws.</summary>
    public static IReadOnlyList<string> Files(string folder)
    {
        var files = new List<string>();
        foreach (var info in new DirectoryInfo(folder).EnumerateFiles())
        {
            if ((info.Attributes & (FileAttributes.Hidden | FileAttributes.System)) == 0 && MediaFiles.IsMedia(info.Name))
            {
                files.Add(info.FullName);
            }
        }

        files.Sort((a, b) => NaturalOrder.Instance.Compare(Path.GetFileName(a), Path.GetFileName(b)));
        return files;
    }

    /// <summary>
    /// The queue a play of <paramref name="paths"/> makes (AM-1.1): one file is played with its folder's other media
    /// files after it, in name order, so the queue is the folder's files and the index is the file's; a folder's
    /// media files from the first; several paths are expanded in that order and the first plays. A file that is not
    /// among its folder's media files (hidden, or not media) is queued alone, so that the player can say why.
    /// </summary>
    public static (IReadOnlyList<string> Paths, int Start) Queue(IReadOnlyList<string> paths)
    {
        if (paths.Count == 1 && File.Exists(paths[0]))
        {
            var file = Path.GetFullPath(paths[0]);
            var folder = Path.GetDirectoryName(file);
            var siblings = folder is null ? [] : Read(folder);
            var index = siblings.ToList().FindIndex(path => string.Equals(path, file, StringComparison.OrdinalIgnoreCase));
            return index >= 0 ? (siblings, index) : ([file], 0);
        }

        var expanded = Expand(paths);
        return (expanded, expanded.Count > 0 ? 0 : -1);
    }

    /// <summary>What an add appends (AM-3.2): each file itself, each folder's media files in order; a path that is gone is left out.</summary>
    public static IReadOnlyList<string> Expand(IEnumerable<string> paths)
    {
        var expanded = new List<string>();
        foreach (var path in paths)
        {
            if (Directory.Exists(path))
            {
                expanded.AddRange(Read(path));
            }
            else if (File.Exists(path))
            {
                expanded.Add(Path.GetFullPath(path));
            }
        }

        return expanded;
    }

    private static IReadOnlyList<string> Read(string folder)
    {
        try
        {
            return Files(folder);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log.Error("A folder could not be read for the queue", ex);
            return [];
        }
    }
}
